-- This table is an isolated development fixture, not a workbook entity or a
-- production contract. A NULL exit time represents a vehicle still parked.
CREATE TABLE "dev_parking_sessions" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "parkingLotId" TEXT NOT NULL,
    "vehicleType" TEXT NOT NULL,
    "testPlate" TEXT NOT NULL,
    "enteredAt" TEXT NOT NULL,
    "exitedAt" TEXT,
    "priceRuleId" TEXT,
    "chargedAmount" REAL,
    "paymentMethod" TEXT,
    FOREIGN KEY ("parkingLotId") REFERENCES "DL_KTHT_baiDoXe" ("Id"),
    FOREIGN KEY ("priceRuleId") REFERENCES "SP_chiTietMucThuKhungGia" ("id"),
    CHECK (julianday("enteredAt") IS NOT NULL),
    CHECK ("exitedAt" IS NULL OR (julianday("exitedAt") IS NOT NULL AND julianday("exitedAt") > julianday("enteredAt"))),
    CHECK (("exitedAt" IS NULL AND "priceRuleId" IS NULL AND "chargedAmount" IS NULL AND "paymentMethod" IS NULL)
        OR ("exitedAt" IS NOT NULL AND "priceRuleId" IS NOT NULL AND "chargedAmount" IS NOT NULL
            AND "chargedAmount" >= 0 AND "paymentMethod" IS NOT NULL AND "paymentMethod" IN ('CASH', 'ELECTRONIC')))
);
