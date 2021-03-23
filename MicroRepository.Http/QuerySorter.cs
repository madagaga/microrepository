namespace MicroRepository.HttpQueryFilter
{
    public class QuerySorter
    {
        public string Name { get; set; }
        public string Order { get; set; }

        public override string ToString()
        {
            return $"{Name} {Order}";
        }
    }
}