
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ha2ne2.DBSimple.SampleModels
{
    public class Book : DBSimpleModel
    {
        [PrimaryKey]
        public int ID { get; set; }
        public string Name { get; set; }
        public int AuthorID { get; set; }
        public int GenreID { get; set; }

        [BelongsTo(typeof(Author), foreignKey: nameof(AuthorID))]
        public Author Author
        {
            get { return Get<Author>(); }
            set { Set(value); }
        }

        [BelongsTo(typeof(Genre), foreignKey: nameof(GenreID))]
        public Genre Genre
        {
            get { return Get<Genre>(); }
            set { Set(value); }
        }
    }
}