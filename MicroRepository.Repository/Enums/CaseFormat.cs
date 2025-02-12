using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroRepository.Repository.Enums
{
    public enum CaseFormat
    {
        Default,    // Garde le format d'origine
        CamelCase,  // exemple: helloWorld
        SnakeCase   // exemple: hello_world
    }
}
