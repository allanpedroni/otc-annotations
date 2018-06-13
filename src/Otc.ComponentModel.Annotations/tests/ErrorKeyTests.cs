using Otc.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Otc.ComponentModel.Annotations.Tests
{
    public class ErrorKeyTests
    {
        private class Model
        {
            [Required(ErrorKey = "2334.1111")]
            public string Prop1 { get; set; }

            [Required]
            public string Prop2 { get; set; }
        }

        private bool Validate(object model, out IEnumerable<ValidationResult> errors)
        {
            var validationContext = new ValidationContext(model, serviceProvider: null, items: null);
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);
            errors = validationResults;

            return isValid;
        }

        [Fact]
        public void Validate_ErrorKey_InvalidModel()
        {
            var model = new Model();
            bool isValid;

            if(!(isValid = Validate(model, out IEnumerable<ValidationResult> errors)))
            {
                Assert.NotEmpty(errors.Where(e => e.ErrorKey == "2334.1111"));
                Assert.NotEmpty(errors.Where(e => e.ErrorKey == "Prop2"));
            }
            else
            {
                Assert.False(isValid);
            }
        }

        [Fact]
        public void Validate_ErrorKey_ValidModel()
        {
            var model = new Model()
            {
                Prop1 = "abc",
                Prop2 = "def"
            };

            Assert.True(Validate(model, out IEnumerable<ValidationResult> errors));
        }
    }
}
