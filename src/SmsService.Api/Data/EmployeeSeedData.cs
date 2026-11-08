using System.Globalization;
using SmsService.Api.Data.Entities;

namespace SmsService.Api.Data;

/// <summary>
/// Demo employees, inserted when the DB is created. In a real system they would come from the HR system.
/// Each row is an employee instance; several rows for the same employee = duplicates, the latest is current.
/// </summary>
public static class EmployeeSeedData
{
    public static readonly Employee[] Employees =
    [
        Row(1,  "ישראל", "ישראלי", "הרצל 10, רחובות",     "0501111111", "2023-01-10 09:00"), // one row
        Row(2,  "שרה",   "כהן",    "הנשיא 5, ירושלים",    "0522222221", "2019-03-01 08:00"), // three rows,
        Row(3,  "שרה",   "כהן",    "הנשיא 5, ירושלים",    "0522222222", "2022-06-15 08:00"), // latest is current
        Row(4,  "שרה",   "כהן",    "הנשיא 5, ירושלים",    "0522222223", "2024-09-20 08:00"),
        Row(5,  "דוד",   "לוי",    "ויצמן 3, בית דגן",    "0533333333", "2021-02-01 10:00"), // same phone twice
        Row(6,  "דוד",   "לוי",    "ויצמן 3, בית דגן",    "0533333333", "2023-05-05 10:00"),
        Row(7,  "משה",   "אברהם",  "הדקל 7, לוד",         "0544444444", "2020-11-11 07:30"), // shared phone
        Row(8,  "רחל",   "אברהם",  "הדקל 7, לוד",         "0544444444", "2021-04-18 07:30"),
        Row(9,  "יוסף",  "מזרחי",  "הגפן 2, חולון",       "0555555555", "2022-02-02 12:00"), // same name,
        Row(10, "יוסף",  "מזרחי",  "התמר 9, אשדוד",       "0556666666", "2022-03-03 12:00"), // different people
        Row(11, "אבי",   "גולן",   "הזית 4, ראשון לציון", "0548888881", "2024-01-01 08:00"), // same date:
        Row(12, "אבי",   "גולן",   "הזית 4, ראשון לציון", "0548888882", "2024-01-01 08:00"), // higher Id wins
        Row(13, "נועה",  "פרץ",    "אחד העם 20, תל אביב", "0587777777", "2025-07-01 09:15"),
        Row(14, "דן",    "כץ",     "הגפן 1, לוד",         "051234",     "2024-03-03 11:00"), // invalid phone
    ];

    private static Employee Row(int id, string first, string last, string address, string phone, string createdAtUtc) => new()
    {
        Id = id,
        FirstName = first,
        LastName = last,
        Address = address,
        Phone = phone,
        CreatedAt = DateTime.SpecifyKind(DateTime.ParseExact(createdAtUtc, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), DateTimeKind.Utc)
    };
}
