// Drives one build of Wiry.Base32 through a case file, so that two builds - or a build and an
// independent oracle - can be compared line by line. The assembly is loaded by path rather than
// referenced: the same probe then runs the released 1.1.1 package and any local build.
//
//   wiry_base32_probe api ASSEMBLY             identity and public/protected surface
//   wiry_base32_probe run ASSEMBLY CASES OUT   one result line per case line
//
// Case line:   id <TAB> op <TAB> codec <TAB> payload <TAB> index <TAB> count
//   op       enc | dec | val
//   codec    std | z | c:<alphabet as UTF-16 hex>:<pad as UTF-16 hex, or ->
//   payload  enc: bytes as hex; dec/val: UTF-16 code units, four hex digits each; "null" for null
//   index    "-" selects the one-argument overload
// Result line: id <TAB> ok|ex <TAB> value (hex as above, enum name, or exception type)
// A codec the build lacks answers NoSuchCodec; one its constructor refuses, Rejected:<exception type>.
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

return args switch
{
    ["api", var dll] => Api.Dump(Probe.Load(dll)),
    ["run", var dll, var cases, var output] => Runner.Run(Probe.Load(dll), cases, output),
    _ => Probe.Usage(),
};

static class Probe
{
    public static Assembly Load(string dll)
    {
        var path = Path.GetFullPath(dll);
        return new AssemblyLoadContext(path).LoadFromAssemblyPath(path);
    }

    public static int Usage()
    {
        Console.Error.WriteLine("usage: wiry_base32_probe api ASSEMBLY | run ASSEMBLY CASES OUT");
        return 2;
    }

    public static string Utf16Hex(string text)
    {
        var sb = new StringBuilder(text.Length * 4);
        foreach (var ch in text)
            sb.Append(((int)ch).ToString("x4"));
        return sb.ToString();
    }

    public static string? FromUtf16Hex(string hex)
    {
        if (hex == "null")
            return null;
        var chars = new char[hex.Length / 4];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = (char)Convert.ToInt32(hex.Substring(i * 4, 4), 16);
        return new string(chars);
    }
}

sealed class Codec
{
    public required Func<byte[], string> EncodeAll { get; init; }
    public required Func<byte[], int, int, string> Encode { get; init; }
    public required Func<string, byte[]> DecodeAll { get; init; }
    public required Func<string, int, int, byte[]> Decode { get; init; }
    public required Func<string, int> ValidateAll { get; init; }
    public required Func<string, int, int, int> Validate { get; init; }
    public required Type ResultEnum { get; init; }

    public static Codec Bind(object instance)
    {
        var type = instance.GetType();
        var target = Expression.Constant(instance);
        var resultEnum = type.GetMethod("Validate", [typeof(string)])!.ReturnType;
        return new Codec
        {
            EncodeAll = Compile<Func<byte[], string>>(target, type, "GetString", typeof(byte[])),
            Encode = Compile<Func<byte[], int, int, string>>(target, type, "GetString", typeof(byte[]), typeof(int), typeof(int)),
            DecodeAll = Compile<Func<string, byte[]>>(target, type, "ToBytes", typeof(string)),
            Decode = Compile<Func<string, int, int, byte[]>>(target, type, "ToBytes", typeof(string), typeof(int), typeof(int)),
            ValidateAll = Compile<Func<string, int>>(target, type, "Validate", typeof(string)),
            Validate = Compile<Func<string, int, int, int>>(target, type, "Validate", typeof(string), typeof(int), typeof(int)),
            ResultEnum = resultEnum,
        };
    }

    // Expression trees rather than MethodInfo.Invoke: the enum return type lives in the loaded
    // assembly and cannot appear in a static delegate type, and Invoke would wrap every
    // exception in TargetInvocationException.
    static T Compile<T>(Expression target, Type type, string name, params Type[] parameters)
    {
        var method = type.GetMethod(name, parameters) ?? throw new MissingMethodException(type.FullName, name);
        var arguments = parameters.Select(Expression.Parameter).ToArray();
        Expression call = Expression.Call(target, method, arguments);
        var returnType = typeof(T).GetGenericArguments()[^1];
        if (call.Type != returnType)
            call = Expression.Convert(call, returnType);
        return Expression.Lambda<T>(call, arguments).Compile();
    }
}

static class Runner
{
    public static int Run(Assembly assembly, string casesPath, string outputPath)
    {
        var baseType = assembly.GetType("Wiry.Base32.Base32Encoding", throwOnError: true)!;
        var codecs = new Dictionary<string, Codec>
        {
            ["std"] = Codec.Bind(baseType.GetProperty("Standard")!.GetValue(null)!),
            ["z"] = Codec.Bind(baseType.GetProperty("ZBase32")!.GetValue(null)!),
        };
        var custom = assembly.GetType("Wiry.Base32.CustomBase32Encoding");
        var rejected = new Dictionary<string, string>();

        using var output = new StreamWriter(outputPath, append: false, new UTF8Encoding(false), 1 << 20);
        output.NewLine = "\n";
        foreach (var line in File.ReadLines(casesPath))
        {
            var f = line.Split('\t');
            var codec = Resolve(codecs, rejected, custom, f[2]);
            string result;
            if (codec == null)
            {
                result = rejected.TryGetValue(f[2], out var reason) ? "ex\tRejected:" + reason : "ex\tNoSuchCodec";
            }
            else
            {
                try
                {
                    result = "ok\t" + Execute(codec, f[1], f[3], f[4], f[5]);
                }
                catch (Exception e)
                {
                    result = "ex\t" + e.GetType().FullName;
                }
            }
            output.Write(f[0]);
            output.Write('\t');
            output.WriteLine(result);
        }
        return 0;
    }

    static Codec? Resolve(Dictionary<string, Codec> codecs, Dictionary<string, string> rejected, Type? custom, string key)
    {
        if (codecs.TryGetValue(key, out var known))
            return known;
        if (custom == null || rejected.ContainsKey(key) || !key.StartsWith("c:", StringComparison.Ordinal))
            return null;
        var parts = key.Split(':');
        var alphabet = Probe.FromUtf16Hex(parts[1]);
        char? pad = parts[2] == "-" ? null : Probe.FromUtf16Hex(parts[2])![0];
        object instance;
        try
        {
            instance = Activator.CreateInstance(custom, alphabet, pad)!;
        }
        catch (TargetInvocationException e)
        {
            rejected[key] = e.InnerException!.GetType().FullName!;
            return null;
        }
        return codecs[key] = Codec.Bind(instance);
    }

    static string Execute(Codec codec, string op, string payload, string index, string count)
    {
        var whole = index == "-";
        switch (op)
        {
            case "enc":
                var bytes = payload == "null" ? null : Convert.FromHexString(payload);
                var encoded = whole ? codec.EncodeAll(bytes!) : codec.Encode(bytes!, int.Parse(index), int.Parse(count));
                return Probe.Utf16Hex(encoded);
            case "dec":
                var text = Probe.FromUtf16Hex(payload);
                var decoded = whole ? codec.DecodeAll(text!) : codec.Decode(text!, int.Parse(index), int.Parse(count));
                return Convert.ToHexString(decoded).ToLowerInvariant();
            case "val":
                var subject = Probe.FromUtf16Hex(payload);
                var verdict = whole ? codec.ValidateAll(subject!) : codec.Validate(subject!, int.Parse(index), int.Parse(count));
                return Enum.GetName(codec.ResultEnum, verdict) ?? verdict.ToString();
            default:
                throw new ArgumentException("unknown op " + op);
        }
    }
}

static class Api
{
    const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                  BindingFlags.Static | BindingFlags.DeclaredOnly;

    public static int Dump(Assembly assembly)
    {
        var name = assembly.GetName();
        var token = name.GetPublicKeyToken();
        Console.WriteLine($"assembly {name.FullName}");
        Console.WriteLine($"strong-named {(token is { Length: > 0 } ? "yes " + Convert.ToHexString(token) : "no")}");
        foreach (var attribute in assembly.GetCustomAttributesData().OrderBy(a => a.AttributeType.FullName, StringComparer.Ordinal))
            Console.WriteLine($"attribute {attribute.AttributeType.Name}({string.Join(", ", attribute.ConstructorArguments.Select(a => a.Value))})");
        foreach (var reference in assembly.GetReferencedAssemblies().OrderBy(r => r.Name, StringComparer.Ordinal))
            Console.WriteLine($"references {reference.FullName}");

        foreach (var type in assembly.GetExportedTypes().OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            var modifiers = type.IsInterface ? "interface" : type.IsEnum ? "enum" :
                type.IsAbstract && type.IsSealed ? "static class" : type.IsAbstract ? "abstract class" :
                type.IsSealed ? "sealed class" : "class";
            var bases = new List<string>();
            if (type.BaseType != null && type.BaseType != typeof(object) && !type.IsEnum)
                bases.Add(type.BaseType.FullName!);
            bases.AddRange(type.GetInterfaces().Select(i => i.FullName!).Order(StringComparer.Ordinal));
            Console.WriteLine($"{modifiers} {type.FullName}{(bases.Count > 0 ? " : " + string.Join(", ", bases) : "")}");
            foreach (var member in type.GetMembers(Declared).Select(Describe).Where(m => m != null).Order(StringComparer.Ordinal))
                Console.WriteLine("    " + member);
        }
        return 0;
    }

    static string? Describe(MemberInfo member)
    {
        switch (member)
        {
            case MethodBase method when Visible(method.IsPublic, method.IsFamily, method.IsFamilyOrAssembly):
                if (method.IsSpecialName && method is MethodInfo { Name: var n } && (n.StartsWith("get_") || n.StartsWith("set_")))
                    return null;
                var parameters = string.Join(", ", method.GetParameters().Select(p => p.ParameterType + " " + p.Name));
                var returns = method is MethodInfo info ? info.ReturnType + " " : "";
                return $"{Access(method.IsPublic)}{Modifiers(method)}{returns}{method.Name}({parameters})";
            case PropertyInfo property:
                var getter = property.GetMethod;
                if (getter == null || !Visible(getter.IsPublic, getter.IsFamily, getter.IsFamilyOrAssembly))
                    return null;
                return $"{Access(getter.IsPublic)}{Modifiers(getter)}{property.PropertyType} {property.Name} {{ get;{(property.SetMethod is { IsPrivate: false, IsAssembly: false } ? " set;" : "")} }}";
            case FieldInfo field when Visible(field.IsPublic, field.IsFamily, field.IsFamilyOrAssembly):
                var constant = field.IsLiteral ? " = " + field.GetRawConstantValue() : "";
                return $"{Access(field.IsPublic)}{(field.IsLiteral ? "const " : field.IsStatic ? "static " : "")}{field.FieldType} {field.Name}{constant}";
            default:
                return null;
        }
    }

    static bool Visible(bool isPublic, bool isFamily, bool isFamilyOrAssembly) => isPublic || isFamily || isFamilyOrAssembly;

    static string Access(bool isPublic) => isPublic ? "public " : "protected ";

    static string Modifiers(MethodBase method) =>
        method.IsStatic ? "static " :
        method is MethodInfo info && info.GetBaseDefinition().DeclaringType != info.DeclaringType ?
            (method.IsFinal ? "sealed override " : "override ") :
        method.IsAbstract ? "abstract " :
        method.IsVirtual && method.IsFinal ? "" :
        method.IsVirtual ? "virtual " : "";
}
