using System;

namespace MicroRepository.Repository.Attributes
{
    public class MapAttribute : Attribute
    {
        public MapAttribute(string name)
        {
            Name = name;
        }
        public string Name { get; private set; }

    }
}
