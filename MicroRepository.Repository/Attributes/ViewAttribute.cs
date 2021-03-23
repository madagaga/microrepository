using System;

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
