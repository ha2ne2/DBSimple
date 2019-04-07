
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ha2ne2.DBSimple.SampleModels
{
    public class CarTran : DBSimpleModel
    {
        [PrimaryKey]
        public int ID { get; set; }
        public DateTime BuyDate { get; set; }
        public int AuthorID { get; set; }
        public int CarID { get; set; }

        [BelongsTo(typeof(Author), foreignKey: nameof(AuthorID))]
        public Author Author
        {
            get { return Get<Author>(); }
            set { Set(value); }
        }

        [BelongsTo(typeof(Car), foreignKey: nameof(CarID))]
        public Car Car
        {
            get { return Get<Car>(); }
            set { Set(value); }
        }
    }
}