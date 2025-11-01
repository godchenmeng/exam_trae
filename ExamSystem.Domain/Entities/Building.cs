using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamSystem.Domain.Entities
{
    /// <summary>
    /// 建筑实体类，映射t_building表
    /// </summary>
    [Table("t_building")]
    public class Building
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// 城市代码
        /// </summary>
        [Column("city")]
        [MaxLength(50)]
        public string? City { get; set; }

        /// <summary>
        /// 城市中文名称
        /// </summary>
        [Column("city_cn")]
        [MaxLength(255)]
        public string? CityCn { get; set; }

        /// <summary>
        /// 机构所在城市
        /// </summary>
        [Column("org_city")]
        [MaxLength(255)]
        public string? OrgCity { get; set; }

        /// <summary>
        /// 机构所在区域
        /// </summary>
        [Column("org_area")]
        [MaxLength(255)]
        public string? OrgArea { get; set; }

        /// <summary>
        /// 机构名称
        /// </summary>
        [Column("org_name")]
        [MaxLength(255)]
        public string? OrgName { get; set; }

        /// <summary>
        /// 机构类型：1-消防队站；2-专职队；3-重点建筑
        /// </summary>
        [Column("org_type")]
        public byte OrgType { get; set; } = 1;

        /// <summary>
        /// 地址
        /// </summary>
        [Column("addr")]
        public string? Address { get; set; }

        /// <summary>
        /// GPS坐标
        /// </summary>
        [Column("gps")]
        [MaxLength(255)]
        public string? Gps { get; set; }

        /// <summary>
        /// 创建者ID
        /// </summary>
        [Column("creator")]
        public int? CreatorId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column("create_date")]
        public DateTime CreateDate { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        [Column("update_date")]
        public DateTime UpdateDate { get; set; } = DateTime.Now;

        /// <summary>
        /// 是否已删除
        /// </summary>
        [Column("deleted")]
        public bool Deleted { get; set; } = false;

        /// <summary>
        /// 高德地图坐标（格式：经度,纬度）
        /// </summary>
        [Column("amap")]
        [MaxLength(255)]
        public string? Amap { get; set; }

        /// <summary>
        /// 地理位置信息
        /// </summary>
        [Column("location")]
        public string? Location { get; set; }

        /// <summary>
        /// 获取机构类型描述
        /// </summary>
        /// <returns>机构类型描述</returns>
        public string GetOrgTypeDescription()
        {
            return OrgType switch
            {
                1 => "消防队站",
                2 => "专职队",
                3 => "重点建筑",
                _ => "未知类型"
            };
        }

        /// <summary>
        /// 获取坐标点（经度，纬度）
        /// </summary>
        /// <returns>坐标点数组，[0]为经度，[1]为纬度</returns>
        public double[]? GetCoordinates()
        {
            if (string.IsNullOrEmpty(Amap))
                return null;

            var parts = Amap.Split(',');
            if (parts.Length != 2)
                return null;

            if (double.TryParse(parts[0], out double lng) && double.TryParse(parts[1], out double lat))
            {
                return new double[] { lng, lat };
            }

            return null;
        }

        /// <summary>
        /// 设置坐标点
        /// </summary>
        /// <param name="longitude">经度</param>
        /// <param name="latitude">纬度</param>
        public void SetCoordinates(double longitude, double latitude)
        {
            Amap = $"{longitude},{latitude}";
        }
    }
}