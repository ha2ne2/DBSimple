using System.Collections.Generic;
using System.Linq;

namespace Ha2ne2.DBSimple.Model
{
    public class User : DBSimpleModel
    {
        [PrimaryKey]
        public int UserID { get; set; }
        public string Name { get; set; }

        [HasMany(typeof(Post), foreignKey: nameof(Post.UserID))]
        public List<Post> Posts {
            get { return Get<List<Post>>(); }
            set { Set(value); }
        }
    }
}
