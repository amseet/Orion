USE orion;

CREATE TABLE tripdata (
tripID INT NOT NULL,
pickup_datetime DATETIME,
dropoff_datetime DATETIME,
passenger_count INT,
trip_distance DOUBLE,
pickup_longitude DOUBLE,
pickup_latitude DOUBLE,
dropoff_longitude DOUBLE,
dropoff_latitude DOUBLE,
fare_amount DOUBLE
)