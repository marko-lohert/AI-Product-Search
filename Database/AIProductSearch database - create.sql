CREATE TABLE [Products] (
  [Id] integer primary key autoincrement,
  [Name] text NOT NULL,
  [Description] text NOT NULL,
  [Price] numeric(16,2),
  [CurrencyISOCode] integer
);


