using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ha2ne2.DBSimple.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ha2ne2.DBSimple;

namespace DBSimple.Tests
{
    [TestClass()]
    public class UtilTests
    {
        //class TestModel
        //{
        //    [PrimaryKey]
        //    public string TestModelID { get; set; }

        //    [HasMany(typeof(int))]
        //    public List<int> X { get; set; }

        //    [PrimaryKey]
        //    public DateTime Date { get; set; }
        //}

        //class TestModel2
        //{
        //    public string TestModelID { get; set; }

        //    public int X { get; set; }

        //    public DateTime Date { get; set; }
        //}

        //[TestMethod()]
        //public void GetPrimaryKeyTest()
        //{
        //    string primaryKey = Ha2ne2.DBSimple.Util.PropertyUtil.GetPrimaryKeyName(typeof(TestModel));

        //    Assert.AreEqual("TestModelID", primaryKey);
        //}

        //[TestMethod()]
        //[ExpectedException(typeof(Exception))]
        //public void ThrowExceptionWhenPrimaryKeyWasNotFound()
        //{
        //    string primaryKey = Ha2ne2.DBSimple.Util.PropertyUtil.GetPrimaryKeyName(typeof(TestModel2));

        //    Assert.Fail();
        //}

        //[TestMethod()]
        //public void GetHasManyClassNameTest()
        //{
        //    string primaryKey = Ha2ne2.DBSimple.Util.PropertyUtil.GetHasManyClassName(typeof(TestModel));

        //    Assert.AreEqual("Int32", primaryKey);
        //}
    }
}