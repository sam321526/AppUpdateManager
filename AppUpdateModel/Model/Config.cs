using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppUpdateModel.Model
{
    public class Config
    {
        /// <summary>
        /// 更新版本
        /// </summary>
        public int Version { set; get; }        
        /// <summary>
        /// 更新清單
        /// </summary>
        public string UpdateList { set; get; }
        /// <summary>
        /// 更新路徑
        /// </summary>
        public string UpdatePath { set; get; }
        /// <summary>
        /// 重啟應用程式名稱
        /// </summary>
        public string Restart { set; get; }
        /// <summary>
        /// 更新內容
        /// </summary>
        public string UpdateContent { set; get; }
        /// <summary>
        /// 更新日期
        /// </summary>
        public DateTime UpdateTime { set; get; }
    }
}
