// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Reflection.Tests
{
    public class AssemblyNameTests
    {
        private const ProcessorArchitecture CurrentMaxValue = ProcessorArchitecture.Arm;

        private static IEnumerable<ProcessorArchitecture> ValidProcessorArchitectureValues()
        {
            return (ProcessorArchitecture[])Enum.GetValues(typeof(ProcessorArchitecture));
        }

        public static IEnumerable<object[]> ProcessorArchitectures_TestData()
        {
            return ValidProcessorArchitectureValues().Select(arch => new object[] { arch });
        }

        public static IEnumerable<object[]> Names_TestData()
        {
            yield return new object[] { "name", "name" };
            yield return new object[] { "NAME", "NAME" };
            yield return new object[] { "name with spaces", "name with spaces" };
            yield return new object[] { "\uD800\uDC00", "\uD800\uDC00" };
            yield return new object[] { "\u043F\u0440\u0438\u0432\u0435\u0442", "\u043F\u0440\u0438\u0432\u0435\u0442" };
            yield return new object[] { "\uD83D\uDC3B\uD83D\uDC3B\uD83D\uDC3B\uD83D\uDC3B\uD83D\uDC3B", "\uD83D\uDC3B\uD83D\uDC3B\uD83D\uDC3B\uD83D\uDC3B\uD83D\uDC3B" };
        }

        public static IEnumerable<object[]> Names_TestDataRequiresEscaping()
        {
            yield return new object[] { " name ", "\" name \"" };
            yield return new object[] { "na,me", "na\\,me" };
            yield return new object[] { "na=me", "na\\=me" };
            yield return new object[] { "na\\me", "na\\\\me" };
            yield return new object[] { "na\'me", "\"na\\'me\"" };
            yield return new object[] { "na\"me", "\"na\\\"me\"" };
            yield return new object[] { "na\tme", "na\\tme" };
            yield return new object[] { "na\0me", "na\0me" };
            yield return new object[] { "na\bme", "na\bme" };
            yield return new object[] { "name\r", "\"name\\r\"" };
            yield return new object[] { "name\n", "\"name\\n\"" };
        }

        [Fact]
        public void Ctor_Empty()
        {
            AssemblyName assemblyName = new AssemblyName();
            Assert.Null(assemblyName.Name);
            Assert.Equal(ProcessorArchitecture.None, assemblyName.ProcessorArchitecture);
        }

        [Theory]
        [MemberData(nameof(Names_TestData))]
        [InlineData(" name ", "name")]
        [InlineData("\tname\t", "name")]
        public void Ctor_String(string name, string expectedName)
        {
            AssemblyName assemblyName = new AssemblyName(name);
            Assert.Equal(expectedName, assemblyName.Name);
            Assert.Equal(ProcessorArchitecture.None, assemblyName.ProcessorArchitecture);
        }

        [Theory]
        [InlineData("MyAssemblyName, Version=1.0.0.0, PublicKeyToken=b77a5c561934e089", "MyAssemblyName, Version=1.0.0.0, PublicKeyToken=b77a5c561934e089")]
        [InlineData("MyAssemblyName, Version=1.0.0.0, PublicKey=00000000000000000400000000000000", "MyAssemblyName, Version=1.0.0.0, PublicKeyToken=b77a5c561934e089")]
        [InlineData("TerraFX.Interop.Windows, PublicKey=" +
            "002400000c800000940000000602000000240000525341310004000001000100897039f5ff762b25b9ba982c3f5836c34e299279c33df505bf806a07bccdf0e1216e661943f557b954cb18422ed522a5" +
            "b3174b85385052677f39c4ce19f30a1ddbaa507054bc5943461651f396afc612cd80419c5ee2b5277571ff65f51d14ba99e4e4196de0f393e89850a465f019dbdc365ed5e81bbafe1370f54efd254ba8",
            "TerraFX.Interop.Windows, PublicKeyToken=35b01b53313a6f7e")]
        public void Ctor_String_Public_Key(string name, string expectedName)
        {
            AssemblyName assemblyName = new AssemblyName(name);
            Assert.Equal(expectedName, assemblyName.FullName);
        }

        [Theory]
        [InlineData(null, typeof(ArgumentNullException))]
        [InlineData("", typeof(ArgumentException))]
        [InlineData("\0", typeof(ArgumentException))]
        [InlineData("\0a", typeof(ArgumentException))]
        [InlineData("           ", typeof(FileLoadException))]
        [InlineData("  \t \r \n ", typeof(FileLoadException))]
        [InlineData("aa, culture=en-en, culture=en-en", typeof(FileLoadException))]
        [InlineData("MyAssemblyName, PublicKey=00000000000000000400000000000000, PublicKeyToken=b77a5c561934e089", typeof(FileLoadException))]
        public void Ctor_String_Invalid(string assemblyName, Type exceptionType)
        {
            Assert.Throws(exceptionType, () => new AssemblyName(assemblyName));
        }

        [Theory]
        [InlineData("aaaa, language=en-en", "aaaa")]
        [InlineData("aaaa, foo=bar, foo=baz", "aaaa")]
        [InlineData("aaaa, foo = bar, foo = bar", "aaaa")]
        [InlineData("aaaa, custom=10", "aaaa")]
        [InlineData("aaaa, custom=10, custom=20", "aaaa")]
        [InlineData("aaaa, custom=lalala", "aaaa")]
        [InlineData("/a", "/a")]
        [InlineData("aa/name ", "aa/name")]
        public void Ctor_String_Valid_Legacy(string name, string expectedName)
        {
            AssemblyName assemblyName = new AssemblyName(name);
            Assert.Equal(expectedName, assemblyName.Name);
        }

        [Theory]
        [InlineData("name\\u50; ", typeof(FileLoadException))]
        [InlineData("aa\\/tname", typeof(FileLoadException))]
        [InlineData("aaaa, publickey=neutral", typeof(FileLoadException))]
        [InlineData("aaaa, publickeytoken=neutral", typeof(FileLoadException))]
        [InlineData("aaaa\0", typeof(FileLoadException))]
        [InlineData("aaaa\0potato", typeof(FileLoadException))]
        [InlineData("aaaa, publickeytoken=null\0,culture=en-en", typeof(FileLoadException))]
        public void Ctor_String_Invalid_Legacy(string assemblyName, Type exceptionType)
        {
            Assert.Throws(exceptionType, () => new AssemblyName(assemblyName));
        }

        [Theory]
        [InlineData("na,me", typeof(FileLoadException))]
        [InlineData("na=me", typeof(FileLoadException))]
        [InlineData("na\'me", typeof(FileLoadException))]
        [InlineData("na\"me", typeof(FileLoadException))]
        public void Ctor_String_Invalid_Issue(string assemblyName, Type exceptionType)
        {
            Assert.Throws(exceptionType, () => new AssemblyName(assemblyName));
        }

        public static IEnumerable<object[]> Ctor_ProcessorArchitecture_TestData()
        {
            // Note that "None" is not valid as part of the name. To get it, the ProcessorArchitecture must be omitted.
            yield return new object[] { "MSIL", ProcessorArchitecture.MSIL };
            yield return new object[] { "msil", ProcessorArchitecture.MSIL };
            yield return new object[] { "mSiL", ProcessorArchitecture.MSIL };
            yield return new object[] { "x86", ProcessorArchitecture.X86 };
            yield return new object[] { "X86", ProcessorArchitecture.X86 };
            yield return new object[] { "IA64", ProcessorArchitecture.IA64 };
            yield return new object[] { "ia64", ProcessorArchitecture.IA64 };
            yield return new object[] { "Ia64", ProcessorArchitecture.IA64 };
            yield return new object[] { "Amd64", ProcessorArchitecture.Amd64 };
            yield return new object[] { "AMD64", ProcessorArchitecture.Amd64 };
            yield return new object[] { "aMd64", ProcessorArchitecture.Amd64 };
            yield return new object[] { "Arm", ProcessorArchitecture.Arm };
            yield return new object[] { "ARM", ProcessorArchitecture.Arm };
            yield return new object[] { "ArM", ProcessorArchitecture.Arm };
        }

        [Theory]
        [MemberData(nameof(Ctor_ProcessorArchitecture_TestData))]
        public void Ctor_ValidArchitectureName_Succeeds(string architectureName, ProcessorArchitecture expected)
        {
            string fullName = "Test, ProcessorArchitecture=" + architectureName;
            AssemblyName assemblyName = new AssemblyName(fullName);
            Assert.Equal(expected, assemblyName.ProcessorArchitecture);
        }

        [Theory]
        [InlineData("None")]
        [InlineData("NONE")]
        [InlineData("NoNe")]
        [InlineData("null")]
        [InlineData("Bogus")]
        [InlineData("")]
        [InlineData("0")]
        [InlineData("1")]
        [InlineData("@!#$!@#$")]
        [InlineData("All your base are belong to us.")]
        public void Ctor_InvalidArchitecture_ThrowsFileLoadException(string invalidName)
        {
            string fullName = "Test, ProcessorArchitecture=" + invalidName;
            Assert.Throws<FileLoadException>(() => new AssemblyName(fullName));
        }

        [Theory]
        [InlineData(AssemblyContentType.Default)]
        [InlineData(AssemblyContentType.WindowsRuntime)]
        public void ContentType(AssemblyContentType contentType)
        {
            AssemblyName assemblyName = new AssemblyName("MyAssemblyName");
            Assert.Equal(AssemblyContentType.Default, assemblyName.ContentType);
            assemblyName.ContentType = contentType;
            Assert.Equal(contentType, assemblyName.ContentType);
        }

        [Fact]
        public void ContentType_CurrentlyExecutingAssembly()
        {
            AssemblyName assemblyName = Helpers.ExecutingAssembly.GetName();
            Assert.Equal(AssemblyContentType.Default, assemblyName.ContentType);
        }

        [Fact]
        public void ContentType_SystemRuntimeAssembly()
        {
            AssemblyName assemblyName = Helpers.ExecutingAssembly.GetName();
            Assert.Equal(AssemblyContentType.Default, assemblyName.ContentType);
        }

        public static IEnumerable<object[]> CultureName_TestData()
        {
            yield return new object[] { new AssemblyName("Test, Culture=en-US"), "en-US", null, null, "Test" };
            yield return new object[] { new AssemblyName("Test, Culture=en-US"), "en-US", "", "", "Test, Culture=neutral" };
            yield return new object[] { new AssemblyName("Test"), null, "en-US", "en-US", "Test, Culture=en-US" };
            yield return new object[] { new AssemblyName("Test"), null, "En-US", "en-US", "Test, Culture=en-US" };
        }

        [Theory]
        [MemberData(nameof(CultureName_TestData))]
        public void CultureName_Set(AssemblyName assemblyName, string originalCultureName, string cultureName, string expectedCultureName, string expectedEqualString)
        {
            Assert.Equal(originalCultureName, assemblyName.CultureName);
            assemblyName.CultureName = cultureName;
            Assert.Equal(expectedCultureName, assemblyName.CultureName);
            Assert.Equal(new AssemblyName(expectedEqualString).FullName, assemblyName.FullName);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/95195", typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnOSX))]
        public void CultureName_Set_Invalid_ThrowsCultureNotFoundException()
        {
            var assemblyName = new AssemblyName("Test");
            Assert.Throws<CultureNotFoundException>(() => new AssemblyName("Test, Culture=NotAValidCulture"));
            Assert.Throws<CultureNotFoundException>(() => assemblyName.CultureName = "NotAValidCulture");
        }

        [Fact]
        public void Verify_CultureName()
        {
            AssemblyName an = new AssemblyName("MyAssemblyName");
            Assert.Null(an.CultureName);
        }
#pragma warning disable SYSLIB0044 // AssemblyName.CodeBase .AssemblyName.EscapedCodeBase are obsolete
        [Fact]
        public void Verify_CodeBase()
        {
            AssemblyName n = new AssemblyName("MyAssemblyName");
            Assert.Null(n.CodeBase);

            n.CodeBase = System.IO.Directory.GetCurrentDirectory();
            Assert.NotNull(n.CodeBase);
        }

        [Fact]
        public static void Verify_EscapedCodeBase()
        {
            AssemblyName n = new AssemblyName("MyAssemblyName");
            Assert.Null(n.EscapedCodeBase);

            n.CodeBase = @"file:///d:/temp/MyAssemblyName1.dll";
            Assert.NotNull(n.EscapedCodeBase);
            Assert.Equal(n.EscapedCodeBase, n.CodeBase);

            n.CodeBase = @"file:///c:/program files/MyAssemblyName.dll";
            Assert.Equal(n.EscapedCodeBase, Uri.EscapeUriString(n.CodeBase));
        }
#pragma warning restore SYSLIB0044

        [Fact]
        public static void Verify_HashAlgorithm()
        {
            AssemblyName an = new AssemblyName("MyAssemblyName");
            Assert.Equal(System.Configuration.Assemblies.AssemblyHashAlgorithm.None, an.HashAlgorithm);

            an.HashAlgorithm = System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA1;
            Assert.Equal(System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA1, an.HashAlgorithm);
        }

        [Fact]
        public static void Verify_VersionCompatibility()
        {
            AssemblyName an = new AssemblyName("MyAssemblyName");
            Assert.Equal(System.Configuration.Assemblies.AssemblyVersionCompatibility.SameMachine, an.VersionCompatibility);

            an.VersionCompatibility = System.Configuration.Assemblies.AssemblyVersionCompatibility.SameProcess;
            Assert.Equal(System.Configuration.Assemblies.AssemblyVersionCompatibility.SameProcess, an.VersionCompatibility);
        }

        [Fact]
        public static void Clone()
        {
            AssemblyName an1 = new AssemblyName("MyAssemblyName");
            an1.Flags = AssemblyNameFlags.PublicKey | AssemblyNameFlags.EnableJITcompileOptimizer;

            object an2 = an1.Clone();
            Assert.Equal(an1.FullName, ((AssemblyName)an2).FullName);
            Assert.Equal(AssemblyNameFlags.PublicKey | AssemblyNameFlags.EnableJITcompileOptimizer, ((AssemblyName)an2).Flags);
        }

        [Fact]
        public static void GetAssemblyName()
        {
            Assembly a = typeof(AssemblyNameTests).Assembly;
            string assemblyLocation = AssemblyPathHelper.GetAssemblyLocation(a);
            if (!string.IsNullOrEmpty(assemblyLocation))
            {
                Assert.Equal(new AssemblyName(a.FullName).ToString(), AssemblyName.GetAssemblyName(assemblyLocation).ToString());
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "File locking is not respected")]
        public static void GetAssemblyName_LockedFile()
        {
            using (var tempFile = new TempFile(Path.GetTempFileName(), 100))
            using (var fileStream = new FileStream(tempFile.Path, FileMode.Append, FileAccess.Write, FileShare.None))
            {
                Assert.Throws<System.IO.IOException>(() => AssemblyName.GetAssemblyName(tempFile.Path));
            }
        }

        public static IEnumerable<object[]> ReferenceMatchesDefinition_TestData()
        {
            yield return new object[] { new AssemblyName(typeof(AssemblyNameTests).Assembly.FullName), new AssemblyName(typeof(AssemblyNameTests).Assembly.FullName), true };
            yield return new object[] { new AssemblyName(typeof(AssemblyNameTests).Assembly.FullName), new AssemblyName("System.Runtime"), false };
        }

        [Theory]
        [MemberData(nameof(ReferenceMatchesDefinition_TestData))]
        public static void ReferenceMatchesDefinition(AssemblyName a1, AssemblyName a2, bool expected)
        {
            Assert.Equal(expected, AssemblyName.ReferenceMatchesDefinition(a1, a2));
        }

        [Theory]
        [InlineData(AssemblyNameFlags.None)]
        [InlineData(AssemblyNameFlags.PublicKey)]
        [InlineData(AssemblyNameFlags.Retargetable)]
        public void Flags(AssemblyNameFlags flags)
        {
            AssemblyName assemblyName = new AssemblyName("MyAssemblyName");
            Assert.Equal(AssemblyNameFlags.None, assemblyName.Flags);
            assemblyName.Flags = flags;
            Assert.Equal(flags, assemblyName.Flags);
        }

        [Fact]
        public void Flags_CurrentlyExecutingAssembly()
        {
            AssemblyName assemblyName = Helpers.ExecutingAssembly.GetName();
            Assert.NotEqual(AssemblyNameFlags.None, assemblyName.Flags);
        }

        [Theory]
        [MemberData(nameof(Names_TestData))]
        public void FullName(string name, string expectedName)
        {
            AssemblyName assemblyName = new AssemblyName(name);
            Assert.Equal(expectedName, assemblyName.FullName);
        }

        [Fact]
        public void FullName_CurrentlyExecutingAssembly()
        {
            AssemblyName assemblyName = typeof(AssemblyNameTests).GetTypeInfo().Assembly.GetName();
            Assert.StartsWith("System.Reflection.Tests", assemblyName.FullName);
            Assert.Equal(assemblyName.Name.Length, assemblyName.FullName.IndexOf(','));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsAssemblyLoadingSupported))]
        public void EmptyFusionLog()
        {
            FileNotFoundException fnfe = Assert.Throws<FileNotFoundException>(() => Assembly.LoadFrom(@"\non\existent\file.dll"));
            Assert.Null(fnfe.FusionLog);
        }

        public static IEnumerable<object[]> SetPublicKey_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new byte[0] };
            yield return new object[] { new byte[16] };
            yield return new object[] { Enumerable.Repeat((byte)'\0', 16).ToArray() };
        }

        [Theory]
        [MemberData(nameof(SetPublicKey_TestData))]
        public void SetPublicKey_GetPublicKey(byte[] publicKey)
        {
            AssemblyName assemblyName = new AssemblyName();
            assemblyName.SetPublicKey(publicKey);
            Assert.Equal(publicKey, assemblyName.GetPublicKey());
        }

        [Theory]
        [MemberData(nameof(SetPublicKey_TestData))]
        public void SetPublicKeyToken_GetPublicKeyToken(byte[] publicKeyToken)
        {
            AssemblyName assemblyName = new AssemblyName();
            assemblyName.SetPublicKeyToken(publicKeyToken);
            Assert.Equal(publicKeyToken, assemblyName.GetPublicKeyToken());
        }

        [Fact]
        public void GetPublicKeyToken_CurrentlyExecutingAssembly()
        {
            AssemblyName assemblyName = typeof(AssemblyNameTests).GetTypeInfo().Assembly.GetName();
            byte[] publicKeyToken = assemblyName.GetPublicKeyToken();
            Assert.Equal(8, publicKeyToken.Length);
        }

        [Theory]
        [MemberData(nameof(Names_TestData))]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData(" name ", " name ")]
        [InlineData("\tname\t", "\tname\t")]
        public void Name_Set(string name, string expectedName)
        {
            AssemblyName assemblyName = new AssemblyName("MyAssemblyName");
            assemblyName.Name = name;
            Assert.Equal(expectedName, assemblyName.Name);
        }

        [Theory]
        [MemberData(nameof(Names_TestData))]
        [MemberData(nameof(Names_TestDataRequiresEscaping))]
        [InlineData(null, "")]
        [InlineData("", "")]
        public void Name_Set_FullName(string name, string expectedName)
        {
            AssemblyName assemblyName = new AssemblyName("MyAssemblyName");
            assemblyName.Name = name;
            Assert.Equal(expectedName, assemblyName.FullName);
        }

        [Fact]
        public void Name_CurrentlyExecutingAssembly()
        {
            AssemblyName assemblyName = typeof(AssemblyNameTests).GetTypeInfo().Assembly.GetName();
            Assert.StartsWith("System.Reflection.Tests", assemblyName.Name);
        }

        // The ECMA replacement key for the Microsoft implementation of the CLR.
        private static readonly byte[] TheKey =
        {
            0x00,0x24,0x00,0x00,0x04,0x80,0x00,0x00,0x94,0x00,0x00,0x00,0x06,0x02,0x00,0x00,
            0x00,0x24,0x00,0x00,0x52,0x53,0x41,0x31,0x00,0x04,0x00,0x00,0x01,0x00,0x01,0x00,
            0x07,0xd1,0xfa,0x57,0xc4,0xae,0xd9,0xf0,0xa3,0x2e,0x84,0xaa,0x0f,0xae,0xfd,0x0d,
            0xe9,0xe8,0xfd,0x6a,0xec,0x8f,0x87,0xfb,0x03,0x76,0x6c,0x83,0x4c,0x99,0x92,0x1e,
            0xb2,0x3b,0xe7,0x9a,0xd9,0xd5,0xdc,0xc1,0xdd,0x9a,0xd2,0x36,0x13,0x21,0x02,0x90,
            0x0b,0x72,0x3c,0xf9,0x80,0x95,0x7f,0xc4,0xe1,0x77,0x10,0x8f,0xc6,0x07,0x77,0x4f,
            0x29,0xe8,0x32,0x0e,0x92,0xea,0x05,0xec,0xe4,0xe8,0x21,0xc0,0xa5,0xef,0xe8,0xf1,
            0x64,0x5c,0x4c,0x0c,0x93,0xc1,0xab,0x99,0x28,0x5d,0x62,0x2c,0xaa,0x65,0x2c,0x1d,
            0xfa,0xd6,0x3d,0x74,0x5d,0x6f,0x2d,0xe5,0xf1,0x7e,0x5e,0xaf,0x0f,0xc4,0x96,0x3d,
            0x26,0x1c,0x8a,0x12,0x43,0x65,0x18,0x20,0x6d,0xc0,0x93,0x34,0x4d,0x5a,0xd2,0x93
        };

        [Fact]
        public static void FullName_WithPublicKey()
        {
            AssemblyName assemblyName = new AssemblyName("MyAssemblyName, Version=1.0.0.0");
            assemblyName.SetPublicKey(TheKey);
            Assert.Equal("MyAssemblyName, Version=1.0.0.0, PublicKeyToken=b03f5f7f11d50a3a", assemblyName.FullName);
        }

        [Fact]
        public static void Name_WithNullPublicKey()
        {
            AssemblyName assemblyName = new AssemblyName("noname,PublicKeyToken=null");
            Assert.Equal(0, assemblyName.GetPublicKeyToken().Length);
            Assert.Equal("noname, PublicKeyToken=null", assemblyName.FullName);
        }

        public static IEnumerable<object[]> Version_TestData()
        {
            yield return new object[] { new Version(255, 1), "255.1" };
            yield return new object[] { new Version(255, 1, 2), "255.1.2" };
            yield return new object[] { new Version(255, 1, 2, 3), "255.1.2.3" };
            yield return new object[] { new Version(1, 2, 0x1ffff, 4), "1.2" };
        }

        [Theory]
        [MemberData(nameof(Version_TestData))]
        public void Version(Version version, string versionString)
        {
            AssemblyName assemblyName = new AssemblyName("MyAssemblyName");
            assemblyName.Version = version;

            string expected = "MyAssemblyName, Version=" + versionString;

            Assert.Equal(expected, assemblyName.FullName);
        }

        [Fact]
        public void Version_CurrentlyExecutingAssembly()
        {
            AssemblyName assemblyName = typeof(AssemblyNameTests).GetTypeInfo().Assembly.GetName();
            assemblyName.Version = new Version(255, 1, 2, 3);
            Assert.Contains("Version=255.1.2.3", assemblyName.FullName);
        }

        private static readonly string VersionUnspecifiedStr = ushort.MaxValue.ToString(NumberFormatInfo.InvariantInfo);

        public static IEnumerable<object[]> Constructor_String_InvalidVersionTest_MemberData()
        {
            // No components
            yield return new object[] { "" };
            yield return new object[] { $"{VersionUnspecifiedStr}" };
            yield return new object[] { $"{VersionUnspecifiedStr}.{VersionUnspecifiedStr}" };
            yield return new object[] { $"{VersionUnspecifiedStr}.{VersionUnspecifiedStr}.{VersionUnspecifiedStr}" };
            yield return new object[] { $"{VersionUnspecifiedStr}.{VersionUnspecifiedStr}.{VersionUnspecifiedStr}.{VersionUnspecifiedStr}" };

            // No major version
            yield return new object[] { $"{VersionUnspecifiedStr}.1" };
            yield return new object[] { $"{VersionUnspecifiedStr}.1.1" };
            yield return new object[] { $"{VersionUnspecifiedStr}.1.1.1" };

            // No minor version
            yield return new object[] { "1" };
            yield return new object[] { $"1.{VersionUnspecifiedStr}" };
            yield return new object[] { $"1.{VersionUnspecifiedStr}.1" };
            yield return new object[] { $"1.{VersionUnspecifiedStr}.1.1" };

            // Too long
            yield return new object[] { "1.1.1.1." };
            yield return new object[] { "1.1.1.1.1" };

            // Invalid component
            foreach (var invalidComponent in new string[] { "", ".", ".1", "-1", "65536", "foo" })
            {
                yield return new object[] { "" + invalidComponent };
                yield return new object[] { "1." + invalidComponent };
                yield return new object[] { "1.1." + invalidComponent };
                yield return new object[] { "1.1.1." + invalidComponent };
            }
        }

        [Theory]
        [MemberData(nameof(Constructor_String_InvalidVersionTest_MemberData))]
        public static void Constructor_String_InvalidVersionTest(string versionStr)
        {
            Assert.Throws<FileLoadException>(() => new AssemblyName("a, Version=" + versionStr));

            if (versionStr.Split('.').Length < 2 || // Version(string) should throw when the minor version is not specified
                (
                    // The Version class has components of size int32, while AssemblyName(string) only allows uint16 values
                    versionStr.IndexOf(VersionUnspecifiedStr, StringComparison.Ordinal) == -1 &&
                    versionStr.IndexOf("65536", StringComparison.Ordinal) == -1
                ))
            {
                Assert.ThrowsAny<Exception>(() => new Version(versionStr));
            }
            else
            {
                new Version(versionStr);
            }
        }

        public static IEnumerable<object[]> Constructor_String_VersionTest_MemberData()
        {
            // No build
            var expectedVersion = new Version(1, 1);
            yield return new object[] { expectedVersion, "1.1" };
            yield return new object[] { expectedVersion, $"1.1.{VersionUnspecifiedStr}" };
            yield return new object[] { expectedVersion, $"1.1.{VersionUnspecifiedStr}.1" };

            // No revision
            expectedVersion = new Version(1, 1, 1);
            yield return new object[] { expectedVersion, "1.1.1" };
            yield return new object[] { expectedVersion, $"1.1.1.{VersionUnspecifiedStr}" };

            // All components
            yield return new object[] { new Version(1, 1, 1, 1), "1.1.1.1" };
            // 65535 causes the component to be considered unspecified. That's not very interesting, so using 65534 instead.
            yield return new object[] { new Version(65534, 65534, 65534, 65534), "65534.65534.65534.65534" };
        }

        [Theory]
        [MemberData(nameof(Constructor_String_VersionTest_MemberData))]
        public static void Constructor_String_VersionTest(Version expectedVersion, string versionStr)
        {
            Assert.NotNull(expectedVersion);

            void Verify(AssemblyName an)
            {
                if (expectedVersion == null)
                {
                    Assert.Null(an.Version);
                }
                else
                {
                    Assert.Equal(expectedVersion, an.Version);
                }
            }

            var assemblyNameFromStr = new AssemblyName("a, Version=" + versionStr);
            Verify(assemblyNameFromStr);
            Verify(new AssemblyName(assemblyNameFromStr.FullName));

            var versionFromStr = new Version(versionStr);

            // The Version class has components of size int32, while AssemblyName(string) only allows uint16 values
            if (versionStr.IndexOf(VersionUnspecifiedStr, StringComparison.Ordinal) == -1)
            {
                Assert.Equal(expectedVersion, versionFromStr);
            }

            assemblyNameFromStr = new AssemblyName("a, Version=" + versionFromStr);
            Verify(assemblyNameFromStr);
            Verify(new AssemblyName(assemblyNameFromStr.FullName));

            assemblyNameFromStr = new AssemblyName() { Name = "a", Version = expectedVersion };
            Verify(assemblyNameFromStr);
            Verify(new AssemblyName(assemblyNameFromStr.FullName));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/27817")]
        public static void Constructor_String_LoadVersionTest()
        {
            string assemblyNamePrefix = "System.Reflection.Tests.Assembly_";

            // Requested version 1.0 does not load 0.0.0.0, but loads 1.2.0.0, 3.0.0.0
            Assert.Throws<FileNotFoundException>(() => Assembly.Load(new AssemblyName(assemblyNamePrefix + "0_0_0_0, Version=1.0")));

            Assert.NotNull(Assembly.Load(new AssemblyName(assemblyNamePrefix + "1_2_0_0, Version=1.0")));
            Assert.NotNull(Assembly.Load(new AssemblyName(assemblyNamePrefix + "3_0_0_0, Version=1.0")));

            // Requested version 1.1 does not load 1.0.0.0, but loads 1.1.2.0, 1.3.0.0
            Assert.Throws<FileNotFoundException>(() => Assembly.Load(new AssemblyName(assemblyNamePrefix + "1_0_0_0, Version=1.1")));
            Assert.NotNull(Assembly.Load(new AssemblyName(assemblyNamePrefix + "1_1_2_0, Version=1.1")));
            Assert.NotNull(Assembly.Load(new AssemblyName(assemblyNamePrefix + "1_3_0_0, Version=1.1")));

            // Requested version 1.1.1 does not load 1.1.0.0, but loads 1.1.1.2, 1.1.3.0
            Assert.Throws<FileNotFoundException>(() => Assembly.Load(new AssemblyName(assemblyNamePrefix + "1_1_0_0, Version=1.1.1")));
            Assert.NotNull(Assembly.Load(new AssemblyName(assemblyNamePrefix + "1_1_1_2, Version=1.1.1")));
            Assert.NotNull(Assembly.Load(new AssemblyName(assemblyNamePrefix + "1_1_3_0, Version=1.1.1")));

            // Requested version 1.1.1.1 does not load 1.1.1.0, but loads 1.1.1.3
            Assert.Throws<FileNotFoundException>(() => Assembly.Load(new AssemblyName(assemblyNamePrefix + "1_1_1_0, Version=1.1.1.1")));
            Assert.NotNull(Assembly.Load(new AssemblyName(assemblyNamePrefix + "1_1_1_3, Version=1.1.1.1")));

            Assert.NotNull(typeof(AssemblyVersion.Program_0_0_0_0));
            Assert.NotNull(typeof(AssemblyVersion.Program_1_0_0_0));
            Assert.NotNull(typeof(AssemblyVersion.Program_1_1_0_0));
            Assert.NotNull(typeof(AssemblyVersion.Program_1_1_1_0));
            Assert.NotNull(typeof(AssemblyVersion.Program_1_1_1_2));
            Assert.NotNull(typeof(AssemblyVersion.Program_1_1_1_3));
            Assert.NotNull(typeof(AssemblyVersion.Program_1_1_2_0));
            Assert.NotNull(typeof(AssemblyVersion.Program_1_1_3_0));
            Assert.NotNull(typeof(AssemblyVersion.Program_1_2_0_0));
            Assert.NotNull(typeof(AssemblyVersion.Program_1_3_0_0));
            Assert.NotNull(typeof(AssemblyVersion.Program_3_0_0_0));
        }

        [Theory]
        [InlineData("Foo")]
        [InlineData("Hi There")]
        public void ToStringTest(string name)
        {
            var assemblyName = new AssemblyName(name);
            Assert.StartsWith(name, assemblyName.ToString());
            Assert.Equal(assemblyName.FullName, assemblyName.ToString());
        }

        [Fact]
        public void ToStringEmptyNameTest()
        {
            var assemblyName = new AssemblyName("test");
            assemblyName.Name = "";
            Assert.StartsWith(string.Empty, assemblyName.ToString());
            Assert.Equal(assemblyName.FullName, assemblyName.ToString());
        }

        [Theory]
        [InlineData((ProcessorArchitecture)(-1))]
        [InlineData((ProcessorArchitecture)int.MaxValue)]
        [InlineData((ProcessorArchitecture)int.MinValue)]
        [InlineData(CurrentMaxValue + 1)]
        [InlineData((ProcessorArchitecture)(~7 | 0))]
        [InlineData((ProcessorArchitecture)(~7 | 1))]
        [InlineData((ProcessorArchitecture)(~7 | 2))]
        [InlineData((ProcessorArchitecture)(~7 | 3))]
        [InlineData((ProcessorArchitecture)(~7 | 4))]
        [InlineData((ProcessorArchitecture)(~7 | 5))]
        [InlineData((ProcessorArchitecture)(~7 | 6))]
        public void SetProcessorArchitecture_InvalidArchitecture_TakesLowerThreeBitsIfLessThanOrEqualToMax(ProcessorArchitecture invalidArchitecture)
        {
            foreach (ProcessorArchitecture validArchitecture in ValidProcessorArchitectureValues())
            {
                var assemblyName = new AssemblyName();
                assemblyName.ProcessorArchitecture = validArchitecture;
                assemblyName.ProcessorArchitecture = invalidArchitecture;

                ProcessorArchitecture maskedInvalidArchitecture = (ProcessorArchitecture)(((int)invalidArchitecture) & 0x7);
                ProcessorArchitecture expectedResult = maskedInvalidArchitecture > CurrentMaxValue ? validArchitecture : maskedInvalidArchitecture;

                Assert.Equal(expectedResult, assemblyName.ProcessorArchitecture);
            }
        }

        [Theory]
        [MemberData(nameof(ProcessorArchitectures_TestData))]
        public void SetProcessorArchitecture_NoneArchitecture_Succeeds(ProcessorArchitecture architecture)
        {
            var assemblyName = new AssemblyName();

            assemblyName.ProcessorArchitecture = architecture;
            assemblyName.ProcessorArchitecture = ProcessorArchitecture.None;

            Assert.Equal(ProcessorArchitecture.None, assemblyName.ProcessorArchitecture);
        }

        [Theory]
        [MemberData(nameof(Ctor_ProcessorArchitecture_TestData))]
        public void GetFullNameAndToString_AreEquivalentAndDoNotPreserveArchitecture(string name, ProcessorArchitecture expected)
        {
            _ = expected;
            string originalFullName = "Test, Culture=en-US, PublicKeyToken=b77a5c561934e089, ProcessorArchitecture=" + name;
            string expectedSerializedFullName = "Test, Culture=en-US, PublicKeyToken=b77a5c561934e089";

            var assemblyName = new AssemblyName(originalFullName);

            Assert.Equal(expectedSerializedFullName, assemblyName.FullName);
            Assert.Equal(expectedSerializedFullName, assemblyName.ToString());
        }

        [Theory]
        [MemberData(nameof(ProcessorArchitectures_TestData))]
        public void SetProcessorArchitecture_ValidArchitecture_Succeeds(ProcessorArchitecture architecture)
        {
            AssemblyName assemblyName = new AssemblyName();
            assemblyName.ProcessorArchitecture = architecture;
            Assert.Equal(architecture, assemblyName.ProcessorArchitecture);
        }
    }
}
