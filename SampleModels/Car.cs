
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ha2ne2.DBSimple.SampleModels
{
    public class Car : DBSimpleModel
    {
        [PrimaryKey]
        public int ID { get; set; }
        public string Name { get; set; }

        [HasMany(typeof(CarTran), foreignKey: nameof(CarTran.CarID))]
        public List<CarTran> CarTranList
        {
            get { return Get<List<CarTran>>(); }
            set { Set(value); }
        }
    }
}