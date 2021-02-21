using System.ComponentModel.DataAnnotations;

namespace GraphQLTest.Model
{
    public class Order
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Customer Customer { get; set; }
    }
}
