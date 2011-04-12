namespace Sample.AspNet.Models
{
    
    public class Index
    {
        public string Name { get; set; }

        public string Posted { get; set; }

        public Index()
        {
            this.Posted = "Nothing :-(";
        }
    }
}