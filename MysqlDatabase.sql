delimiter $$

CREATE DATABASE `catchall` /*!40100 DEFAULT CHARACTER SET utf8 */$$

delimiter $$

use catchall$$

CREATE TABLE `blocked` (
  `idblocked` int(11) NOT NULL AUTO_INCREMENT,
  `address` varchar(45) NOT NULL,
  `date` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `hits` int(11) NOT NULL DEFAULT '0',
  PRIMARY KEY (`idblocked`),
  UNIQUE KEY `address_UNIQUE` (`address`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8$$

delimiter $$

CREATE TABLE `cought` (
  `idCought` int(11) NOT NULL AUTO_INCREMENT,
  `date` datetime NOT NULL,
  `original` varchar(255) NOT NULL,
  `replaced` varchar(255) NOT NULL,
  `subject` varchar(255) DEFAULT NULL,
  `message_id` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`idCought`)
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8$$

