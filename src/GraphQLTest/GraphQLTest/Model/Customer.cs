using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GraphQLTest.Model
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public ICollection<Order> Orders { get; set; }
    }
}
