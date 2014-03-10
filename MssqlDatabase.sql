use catchall;

CREATE TABLE blocked (
  idblocked int NOT NULL IDENTITY(1,1),
  address nvarchar(45) NOT NULL,
  date datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  hits int NOT NULL DEFAULT '0',
  PRIMARY KEY (idblocked),
  UNIQUE (address)
);

CREATE TABLE caught (
  idCaught int NOT NULL IDENTITY(1,1),
  date datetime NOT NULL,
  original nvarchar(255) NOT NULL,
  replaced nvarchar(255) NOT NULL,
  subject nvarchar(255) DEFAULT NULL,
  message_id nvarchar(255) DEFAULT NULL,
  PRIMARY KEY (idCought)
);