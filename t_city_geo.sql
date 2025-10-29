/*
 Navicat Premium Dump SQL

 Source Server         : 本机MySql数据库
 Source Server Type    : MySQL
 Source Server Version : 80011 (8.0.11)
 Source Host           : localhost:3306
 Source Schema         : uexam

 Target Server Type    : MySQL
 Target Server Version : 80011 (8.0.11)
 File Encoding         : 65001

 Date: 26/10/2025 23:45:27
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for t_city_geo
-- ----------------------------
DROP TABLE IF EXISTS `t_city_geo`;
CREATE TABLE `t_city_geo` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `city` varchar(255) COLLATE utf8_unicode_ci DEFAULT NULL,
  `coordinates` longtext CHARACTER SET utf8 COLLATE utf8_unicode_ci,
  `gbCode` varchar(255) COLLATE utf8_unicode_ci DEFAULT NULL,
  `geom` geometry DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;
