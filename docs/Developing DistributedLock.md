# Developing DistributedLock

## Installing back-ends for testing

DistributedLock has a variety of back-ends; to be able to develop and run tests against all of them you'll need to install a good amount of software.

### Azure

For the Azure back-end, [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite) is used for local development. See [here](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio%2Cblob-storage#install-azurite) for how to install.

### MySQL

The MySQL driver covers both MySQL and MariaDB; so we'll need to install both.

#### MariaDB

The MariaDB installer can be downloaded [here](https://mariadb.org/download/?t=mariadb&p=mariadb&os=windows&cpu=x86_64&pkg=msi&m=acorn).

After downloading, you'll need to enable the performance_schema which is used by DistributedLock's tests. You can do this by adding the following to your my.ini/my.cnf file (C:\Program Files\MariaDB {version}\data\my.ini on windows):

```ini
# activates the performance_schema tables which are needed by DistributedLock tests
performance_schema=ON
```

After doing this, restart MariaDB (on Windows, do this in the Services app).

Next, create the `distributed_lock` database and a user for the tests to run as:
```sql
CREATE DATABASE distributed_lock;
CREATE USER 'DistributedLock'@'localhost' IDENTIFIED BY '<password>';
GRANT ALL PRIVILEGES ON distributed_lock.* TO 'DistributedLock'@'localhost';
GRANT SELECT ON performance_schema.* TO 'DistributedLock'@'localhost';
```

Finally, add your username (DistributedLock) and password to `DistributedLock.Tests/credentials/mariadb.txt`, with the username on line 1 and the password on line 2.

#### MySQL

You can install MySQL from [here](https://dev.mysql.com/downloads/mysql/). Run on port 3307 to avoid conflicting with MariaDB.

Add your username and password to `DistributedLock.Tests/credentials/mysql.txt`, with the username on line 1 and the password on line 2.

### Oracle

You can install Oracle from [here](https://www.oracle.com/database/technologies/oracle-database-software-downloads.html#db_free). It claims not to support Windows 11 Home, but it seems to install and work fine.

Add your username (e.g. SYSTEM) and password to `DistributedLock.Tests/credentials/oracle.txt`, with the username on line 1 and the password on line 2.

If the Oracle tests fail with `ORA-12541: TNS:no listener`, you may have to start the OracleOraDB21Home1TNSListener service in services.svc and/or restart the OracleServiceXE. After starting these it can take a few minutes for the DB to come online.

### Postgres

Have docker installed, we are using https://testcontainers.com/modules/postgresql/

### SQL Server

Have docker installed, we are using https://testcontainers.com/modules/mssql/

### Redis

Have docker installed, we are using https://testcontainers.com/modules/redis/

### ZooKeeper

Download a ZooKeeper installation by going to [https://zookeeper.apache.org/](https://zookeeper.apache.org/)->Documentation->Release ...->Getting Started->Download->stable.

Extract the zip archive, and within it copy `zoo_sample.cfg` to `zoo.cfg`.

Add the full path of the extracted directory (the one containing README.md, bin, conf, etc) to `DistributedLock.Tests/credentials/zookeeper.txt` as a single line.

Also, install Java Development Kit (JDK) because ZooKeeper runs on Java.
