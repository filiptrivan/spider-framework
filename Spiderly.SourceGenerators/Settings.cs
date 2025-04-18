﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Spiderly.SourceGenerators
{
    public static class Settings
    {
        public static int NumberOfPropertiesWithoutAdditionalManyToManyProperties = 2;

        public static string HttpOptionsBase = ", this.config.httpOptions";
        public static string HttpOptionsSkipSpinner = ", this.config.httpSkipSpinnerOptions";
        public static string HttpOptionsText = ", { ...this.config.httpOptions, responseType: 'text' }";
        public static string HttpOptionsBlob = ", { observe: 'response', responseType: 'blob' }";
    }
}
