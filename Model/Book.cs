
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ha2ne2.DBSimple.SampleModels
{
    public class Book : DBSimpleModel
    {
        [PrimaryKey]
        public int BookID { get; set; }
        public int AuthorID { get; set; }
        public string Title { get; set; }

        [BelongsTo(typeof(Author), foreignKey: nameof(AuthorID))]
        public Author Author
        {
            get { return Get<Author>(); }
            set { Set(value); }
        }
    }
}