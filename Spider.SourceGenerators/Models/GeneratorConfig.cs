using System;
using System.Collections.Generic;
using System.Text;

namespace Spider.SourceGenerators.Models
{
    public class GeneratorConfig
    {
        public Generator NgControllersGenerator { get; set; }
        public Generator NgEntitiesGenerator { get; set; }
        public Generator NgEnumsGenerator { get; set; }
        public Generator NgTranslatesGenerator { get; set; }
        public Generator NgValidatorsGenerator { get; set; }
        public Generator EntitiesToDTOGenerator { get; set; }
        public Generator FluentValidationGenerator { get; set; }
    }

    public class Generator
    {
        public string Output { get; set; }
    }
}
