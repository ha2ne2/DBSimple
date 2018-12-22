using System.Collections.Generic;
using System.Linq;

namespace Ha2ne2.DBSimple.SampleModels
{
    public class Author : DBSimpleModel
    {
        [PrimaryKey]
        public int AuthorID { get; set; }
        public string Name { get; set; }

        [HasMany(typeof(Book), foreignKey: nameof(Book.AuthorID))]
        public List<Book> Books {
            get { return Get<List<Book>>(); }
            set { Set(value); }
        }
    }
}
