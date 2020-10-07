using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroRepository.Repository.Attributes
{
    public class ViewAttribute : Attribute
    {
        public ViewAttribute(string name)
        {
            Name = name;
        }
        public string Name { get; private set; }

    }
}
