using System;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace EduCore.Plugins.Tests
{
    public class EduKeyTests
    {
        private static readonly Guid A = new Guid("11111111-1111-1111-1111-111111111111");
        private static readonly Guid B = new Guid("22222222-2222-2222-2222-222222222222");

        [Fact]
        public void Joins_parts_with_separator()
        {
            Assert.Equal("11111111-1111-1111-1111-111111111111|22222222-2222-2222-2222-222222222222", EduKey.Build(A, B));
        }

        [Fact]
        public void Entity_reference_and_guid_produce_the_same_token()
        {
            Assert.Equal(EduKey.Build(A), EduKey.Build(new EntityReference("edu_student", A)));
        }

        [Fact]
        public void Dates_are_date_only_and_culture_invariant()
        {
            Assert.Equal("2026-09-25", EduKey.Build(new DateTime(2026, 9, 25, 14, 30, 0)));
        }

        [Fact]
        public void Text_is_trimmed_and_lower_cased()
        {
            Assert.Equal("5-a", EduKey.Build("  5-A "));
        }

        [Fact]
        public void Choice_and_int_use_their_numeric_value()
        {
            Assert.Equal("85968000|7", EduKey.Build(new OptionSetValue(85968000), 7));
        }

        [Fact]
        public void Optional_part_uses_the_fallback_token_when_empty()
        {
            Assert.Equal(A.ToString("D") + "|daily", EduKey.Build(A, EduKey.Or(null, "daily")));
            Assert.Equal(A.ToString("D") + "|daily", EduKey.Build(A, EduKey.Or("  ", "daily")));
        }

        [Fact]
        public void Optional_part_keeps_a_real_value()
        {
            Assert.Equal(A.ToString("D") + "|" + B.ToString("D"), EduKey.Build(A, EduKey.Or(B, "daily")));
        }

        [Fact]
        public void Null_part_is_rejected()
        {
            Assert.Throws<ArgumentException>(() => EduKey.Build(A, null));
        }

        [Fact]
        public void Empty_text_part_is_rejected()
        {
            Assert.Throws<ArgumentException>(() => EduKey.Build(A, "   "));
        }

        [Fact]
        public void Text_containing_the_separator_is_rejected()
        {
            Assert.Throws<ArgumentException>(() => EduKey.Build("a|b"));
        }

        [Fact]
        public void Unsupported_type_is_rejected()
        {
            Assert.Throws<ArgumentException>(() => EduKey.Build(1.5m));
        }

        [Fact]
        public void No_parts_is_rejected()
        {
            Assert.Throws<ArgumentException>(() => EduKey.Build());
        }

        [Fact]
        public void Different_inputs_produce_different_keys()
        {
            Assert.NotEqual(EduKey.Build(A, B), EduKey.Build(B, A));
        }
    }
}