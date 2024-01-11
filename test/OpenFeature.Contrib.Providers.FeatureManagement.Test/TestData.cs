using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenFeature.Contrib.Providers.FeatureManagement.Test
{
    internal class TestData
    {
        private static string VALID_EMAIL = "test.user@openfeature.dev";
        private static string VALID_GROUP = "test.group";
        private static string INVALID_EMAIL = "missing.user@openfeature.dev";
        private static string INVALID_GROUP = "missing.group";

        public static IEnumerable<object[]> BooleanNoContext()
        {
            yield return new object[] { "MissingFlagKey", true, true };
            yield return new object[] { "Flag_Boolean_AlwaysOn", false, true };
            yield return new object[] { "Flag_Boolean_AlwaysOff", true, false };
        }

        public static IEnumerable<object[]> BooleanWithContext()
        {
            yield return new object[] { "MissingFlagKey", VALID_EMAIL, VALID_GROUP, true, true };
            yield return new object[] { "Flag_Boolean_TargetingUserId", VALID_EMAIL, VALID_GROUP, false, true };
            yield return new object[] { "Flag_Boolean_TargetingUserId", INVALID_EMAIL, INVALID_GROUP, true, false };
            yield return new object[] { "Flag_Boolean_TargetingGroup", VALID_EMAIL, VALID_GROUP, false, true };
            yield return new object[] { "Flag_Boolean_TargetingGroup", INVALID_EMAIL, INVALID_GROUP, true, false };
        }

        public static IEnumerable<object[]> DoubleNoContext()
        {
            yield return new object[] { "MissingFlagKey", 1.0, 1.0 };
            yield return new object[] { "Flag_Double_AlwaysOn", 0.0, 1.0 };
            yield return new object[] { "Flag_Double_AlwaysOff", 0.0, -1.0 };
        }

        public static IEnumerable<object[]> DoubleWithContext()
        {
            yield return new object[] { "MissingFlagKey", VALID_EMAIL, VALID_GROUP, 1.0, 1.0 };
            yield return new object[] { "Flag_Double_TargetingUserId", VALID_EMAIL, VALID_GROUP, 0.0, 1.0};
            yield return new object[] { "Flag_Double_TargetingUserId", INVALID_EMAIL, INVALID_GROUP, 0.0, -1.0};
            yield return new object[] { "Flag_Double_TargetingGroup", VALID_EMAIL, VALID_GROUP, 0.0, 1.0 };
            yield return new object[] { "Flag_Double_TargetingGroup", INVALID_EMAIL, INVALID_GROUP, 0.0, -1.0};
        }

        public static IEnumerable<object[]> IntegerNoContext()
        {
            yield return new object[] { "MissingFlagKey", 1, 1 };
            yield return new object[] { "Flag_Integer_AlwaysOn", 0, 1 };
            yield return new object[] { "Flag_Integer_AlwaysOff", 0, -1 };
        }

        public static IEnumerable<object[]> IntegerWithContext()
        {
            yield return new object[] { "MissingFlagKey", VALID_EMAIL, VALID_GROUP, 1, 1 };
            yield return new object[] { "Flag_Integer_TargetingUserId", VALID_EMAIL, VALID_GROUP, 0, 1 };
            yield return new object[] { "Flag_Integer_TargetingUserId", INVALID_EMAIL, INVALID_GROUP, 0, -1 };
            yield return new object[] { "Flag_Integer_TargetingGroup", VALID_EMAIL, VALID_GROUP, 0, 1 };
            yield return new object[] { "Flag_Integer_TargetingGroup", INVALID_EMAIL, INVALID_GROUP, 0, -1 };
        }

        public static IEnumerable<object[]> StringNoContext()
        {
            yield return new object[] { "MissingFlagKey", "DefaultValue", "DefaultValue" };
            yield return new object[] { "Flag_String_AlwaysOn", "DefaultValue", "FlagEnabled" };
            yield return new object[] { "Flag_String_AlwaysOff", "DefaultValue", "FlagDisabled" };
        }

        public static IEnumerable<object[]> StringWithContext()
        {
            yield return new object[] { "MissingFlagKey", VALID_EMAIL, VALID_GROUP, "DefaultValue", "DefaultValue" };
            yield return new object[] { "Flag_String_TargetingUserId", VALID_EMAIL, VALID_GROUP, "DefaultValue", "FlagEnabled" };
            yield return new object[] { "Flag_String_TargetingUserId", INVALID_EMAIL, INVALID_GROUP, "DefaultValue", "FlagDisabled" };
            yield return new object[] { "Flag_String_TargetingGroup", VALID_EMAIL, VALID_GROUP, "DefaultValue", "FlagEnabled" };
            yield return new object[] { "Flag_String_TargetingGroup", INVALID_EMAIL, INVALID_GROUP, "DefaultValue", "FlagDisabled" };
        }

        public static IEnumerable<object[]> StructureNoContext()
        {
            yield return new object[] { "Flag_Structure_AlwaysOn" };
            yield return new object[] { "Flag_Structure_AlwaysOff" };
        }

        public static IEnumerable<object[]> StructureWithContext()
        {
            yield return new object[] { "Flag_Structure_TargetingUserId", "test.user@openfeature.dev", "test.group" };
            yield return new object[] { "Flag_Structure_TargetingGroup", "test.user@openfeature.dev", "test.group" };
            yield return new object[] { "Flag_Structure_TargetingUserId", "missing.user@openfeature.dev", "missing.group" };
            yield return new object[] { "Flag_Structure_TargetingGroup", "missing.user@openfeature.dev", "missing.group" };
        }
    }
}
