USE orion;

CREATE TABLE tempDB (
	VendorID int NULL,
	tpep_pickup_datetime datetime NULL,
	tpep_dropoff_datetime datetime NULL,
	passenger_count int NULL,
	trip_distance float NULL,
	pickup_longitude float NULL,
	pickup_latitude float NULL,
	RatecodeID int NULL,
	store_and_fwd_flag nvarchar(50) NULL,
	dropoff_longitude float NULL,
	dropoff_latitude float NULL,
	payment_type int NULL,
	fare_amount float NULL,
	extra float NULL,
	mta_tax float NULL,
	tip_amount float NULL,
	tolls_amount float NULL,
	improvement_surcharge float NULL,
	total_amount varchar(10) NULL
);


LOAD DATA INFILE 'C:/Users/seetam/Documents/TaxiData/Raw/yellow_tripdata_2015-01.csv'
INTO TABLE tempDB
COLUMNS TERMINATED BY ','
OPTIONALLY ENCLOSED BY '"'
ESCAPED BY '"'
LINES TERMINATED BY '\n'
IGNORE 1 LINES;


INSERT INTO tripdata (tripdata.pickup_datetime, tripdata.dropoff_datetime, tripdata.trip_distance, tripdata.passenger_count,
							tripdata.pickup_longitude, tripdata.pickup_latitude, tripdata.dropoff_longitude, tripdata.dropoff_latitude,
							tripdata.fare_amount)
		Select tpep_pickup_datetime, tpep_dropoff_datetime, trip_distance, passenger_count,
							pickup_longitude, pickup_latitude, dropoff_longitude, dropoff_latitude,
							fare_amount from tempDB;
                            
DROP TABLE tempDB;