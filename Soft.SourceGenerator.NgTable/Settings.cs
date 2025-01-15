using System;
using System.Collections.Generic;
using System.Text;

namespace Soft.SourceGenerators
{
    public static class Settings
    {
        public static int NumberOfPropertiesWithoutAdditionalManyToManyProperties = 2;

        public static string HttpOptionsBase = ", environment.httpOptions";
        public static string HttpOptionsSkipSpinner = ", environment.httpSkipSpinnerOptions";
        public static string HttpOptionsText = ", { ...environment.httpOptions, responseType: 'text' }";
        public static string HttpOptionsBlob = ", { observe: 'response', responseType: 'blob' }";
    }
}
