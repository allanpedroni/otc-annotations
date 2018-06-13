using Otc.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnnotationsSampleApp
{
    public class Model
    {
        [Required(ErrorKey = "2334.1111")]
        public string Name { get; set; }
    }
}
