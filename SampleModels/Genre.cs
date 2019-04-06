using System.Collections.Generic;
using System.Linq;

namespace Ha2ne2.DBSimple.SampleModels
{
    public class Genre : DBSimpleModel
    {
        [PrimaryKey]
        public int ID { get; set; }
        public string Name { get; set; }

        [HasMany(typeof(Book), foreignKey: nameof(Book.GenreID))]
        public List<Book> Books {
            get { return Get<List<Book>>(); }
            set { Set(value); }
        }
    }
}
