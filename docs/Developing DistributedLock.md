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

(Windows) If you don't want MariaDB always running on your machine, set the Startup type to "Manual" for `MariaDB`.

Finally, add your username (DistributedLock) and password to `DistributedLock.Tests/credentials/mariadb.txt`, with the username on line 1 and the password on line 2.

#### MySQL

You can install MySQL from [here](https://dev.mysql.com/downloads/mysql/). Run on port 3307 to avoid conflicting with MariaDB.

(Windows) If you don't want MySQL always running on your machine, set the Startup type to "Manual" for `MySQL{Version}`.

Add your username and password to `DistributedLock.Tests/credentials/mysql.txt`, with the username on line 1 and the password on line 2.

### Oracle

You can install Oracle from [here](https://www.oracle.com/database/technologies/oracle-database-software-downloads.html#db_free). It claims not to support Windows 11 Home, but it seems to install and work fine.

Add your username (e.g. SYSTEM) and password to `DistributedLock.Tests/credentials/oracle.txt`, with the username on line 1 and the password on line 2.

(Windows) If the Oracle tests fail with `ORA-12541: TNS:no listener`, you may have to start the `OracleOraDB21Home1TNSListener` service in services.svc and/or restart the `OracleServiceXE`. After starting these it can take a few minutes for the DB to come online.

### Postgres

You can install Postgres from [here](https://www.enterprisedb.com/downloads/postgres-postgresql-downloads).

In `C:\Program Files\PostgreSQL\<version>\data\postgresql.conf`, update `max_connections` to 200.

(Windows) If you don't want Postgres always running on your machine, set the Startup type to "Manual" for `postgresql-x64-{VERSION} - PostgresSQL Server {VERSION}`.

Add your username (e.g. postgres) and password to `DistributedLock.Tests/credentials/postgres.txt`, with the username on line 1 and the password on line 2.

### SQL Server

Download SQL developer edition from [here](https://www.microsoft.com/en-us/sql-server/sql-server-downloads).

(Windows) If you don't want SQLServer always running on your machine, set the Startup type to "Manual" for `SQL Server (MSSQLSERVER)`.

The tests connect via integrated security. 

### Redis

Install Redis locally. On Windows, install it via WSL as described [here](https://developer.redis.com/create/windows/).

You do not need it running as a service: the tests will start and stop instances automatically.


### MongoDB

The recommended approach for MongoDB is to use Docker (e.g. Docker desktop on Windows). To spin up an instance without manual initialization. The test suite assumes docker is running and will try to start the container with:

```bat
docker run -d -p 27017:27017 --name distributed-lock-mong mongo:latest
```

<details>
<summary>Advanced setup options</summary>
<p>
You can download the MongoDB Community Server from [here](https://www.mongodb.com/try/download/community).

Or use `docker compose` to start a replica set environment:

```yaml
services:
  mongo_primary:
    image: bitnami/mongodb:latest
    container_name: mongo_primary
    environment:
      - TZ=UTC
      - MONGODB_ADVERTISED_HOSTNAME=host.docker.internal
      - MONGODB_REPLICA_SET_MODE=primary
      - MONGODB_REPLICA_SET_NAME=rs0
      - MONGODB_ROOT_USER=yourUsername
      - MONGODB_ROOT_PASSWORD=yourPassword
      - MONGODB_REPLICA_SET_KEY=yourKey
    ports:
      - "27017:27017"
    volumes:
      - "mongodb_master_data:/bitnami/mongodb"

  mongo_secondary:
    image: bitnami/mongodb:latest
    container_name: mongo_secondary
    depends_on:
      - mongo_primary
    environment:
      - TZ=UTC
      - MONGODB_ADVERTISED_HOSTNAME=host.docker.internal
      - MONGODB_REPLICA_SET_MODE=secondary
      - MONGODB_REPLICA_SET_NAME=rs0
      - MONGODB_INITIAL_PRIMARY_PORT_NUMBER=27017
      - MONGODB_INITIAL_PRIMARY_HOST=host.docker.internal
      - MONGODB_INITIAL_PRIMARY_ROOT_USER=yourUsername
      - MONGODB_INITIAL_PRIMARY_ROOT_PASSWORD=yourPassword
      - MONGODB_REPLICA_SET_KEY=yourKey
    ports:
      - "27018:27017"

  mongo_arbiter:
    image: bitnami/mongodb:latest
    container_name: mongo_arbiter
    depends_on:
      - mongo_primary
    environment:
      - TZ=UTC
      - MONGODB_ADVERTISED_HOSTNAME=host.docker.internal
      - MONGODB_REPLICA_SET_MODE=arbiter
      - MONGODB_REPLICA_SET_NAME=rs0
      - MONGODB_INITIAL_PRIMARY_PORT_NUMBER=27017
      - MONGODB_INITIAL_PRIMARY_HOST=host.docker.internal
      - MONGODB_INITIAL_PRIMARY_ROOT_USER=yourUsername
      - MONGODB_INITIAL_PRIMARY_ROOT_PASSWORD=yourPassword
      - MONGODB_REPLICA_SET_KEY=yourKey
    ports:
      - "27019:27017"

volumes:
  mongodb_master_data:
    driver: local
```

The tests default to `mongodb://localhost:27017`. To use a custom connection string (e.g. for credentials), place it in `DistributedLock.Tests/credentials/mongodb.txt`.

If you're using a replica set or sharded cluster, your connection string might look like this:

```
mongodb://yourUsername:yourPassword@host.docker.internal:27017,host.docker.internal:27018,host.docker.internal:27019/?replicaSet=rs0&authSource=admin&serverSelectionTimeoutMS=1000
```
</p>
</details>

### ZooKeeper

Download a ZooKeeper installation by going to [https://zookeeper.apache.org/](https://zookeeper.apache.org/)->Documentation->Release ...->Getting Started->Download->stable.

Extract the zip archive, and within it copy `zoo_sample.cfg` to `zoo.cfg`.

Add the full path of the extracted directory (the one containing README.md, bin, conf, etc) to `DistributedLock.Tests/credentials/zookeeper.txt` as a single line.

Also, install Java Development Kit (JDK) because ZooKeeper runs on Java.
