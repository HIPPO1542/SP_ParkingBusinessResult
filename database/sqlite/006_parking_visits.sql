-- Operational visits imported from files. This table is an approved extension
-- and is separate from dev_parking_sessions, which contains demo data only.
CREATE TABLE parking_visit (
    id TEXT NOT NULL PRIMARY KEY,
    parkingLotId TEXT NOT NULL REFERENCES "DL_KTHT_baiDoXe"("Id"),
    licensePlate TEXT NOT NULL COLLATE NOCASE,
    vehicleType TEXT NOT NULL,
    enteredAt TEXT NOT NULL,
    exitedAt TEXT,
    collectedAmount TEXT,
    paymentMethod TEXT,
    createdAt TEXT NOT NULL,
    updatedAt TEXT NOT NULL,
    UNIQUE (parkingLotId, licensePlate, enteredAt),
    CHECK (length(trim(licensePlate)) > 0),
    CHECK (length(trim(vehicleType)) > 0),
    CHECK (exitedAt IS NULL OR exitedAt > enteredAt)
);
CREATE INDEX ix_parking_visit_exit ON parking_visit (parkingLotId, exitedAt);
