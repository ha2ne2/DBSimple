
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ha2ne2.DBSimple.Model
{
    public class Post : DBSimpleModel
    {
        [PrimaryKey]
        public int PostID { get; set; }
        public int UserID { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }

        [BelongsTo(typeof(User), foreignKey: nameof(UserID))]
        public User User
        {
            get { return Get<User>(); }
            set { Set(value); }
        }
    }
}