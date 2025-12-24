using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BookinhMVC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class BookAppointmentModel : PageModel
{
    private readonly BookingContext _context;
    public BookAppointmentModel(BookingContext context) => _context = context;

    [BindProperty]
    public int SelectedDoctorId { get; set; }
    [BindProperty]
    public int SelectedPatientId { get; set; }
    [BindProperty]
    public DateTime SelectedDate { get; set; }
    [BindProperty]
    public string SelectedTime { get; set; }
    [BindProperty]
    public string Symptoms { get; set; }
    public List<BacSi> Doctors { get; set; }
    public List<DateTime> AvailableDates { get; set; } = new();
    public List<string> AvailableTimes { get; set; } = new();
    public string Message { get; set; }

    public async Task OnGetAsync()
    {
        Doctors = await _context.BacSis.Include(b => b.Khoa).ToListAsync();
    }

    public async Task<JsonResult> OnGetAvailableDatesAsync(int doctorId)
    {
        var dates = await _context.LichLamViecs
            .Where(l => l.MaBacSi == doctorId && l.TrangThai == "Đã xác nhận" && l.NgayLamViec >= DateTime.Today)
            .Select(l => l.NgayLamViec.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();
        return new JsonResult(dates);
    }

    public async Task<JsonResult> OnGetAvailableTimesAsync(int doctorId, DateTime date)
    {
        var schedules = await _context.LichLamViecs
            .Where(l => l.MaBacSi == doctorId && l.NgayLamViec == date && l.TrangThai == "Đã xác nhận")
            .ToListAsync();

        var bookedTimes = await _context.LichHens
            .Where(l => l.MaBacSi == doctorId && l.NgayGio.Date == date && l.TrangThai != "Đã hủy")
            .Select(l => l.NgayGio.TimeOfDay)
            .ToListAsync();

        var times = new List<string>();
        foreach (var sch in schedules)
        {
            for (var t = sch.GioBatDau; t < sch.GioKetThuc; t = t.Add(TimeSpan.FromMinutes(30)))
            {
                if (!bookedTimes.Contains(t))
                    times.Add(t.ToString(@"hh\:mm"));
            }
        }
        return new JsonResult(times);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Validate input (add more as needed)
        if (SelectedDoctorId == 0 || SelectedPatientId == 0 || SelectedDate == default || string.IsNullOrEmpty(SelectedTime))
        {
            Message = "Vui lòng điền đầy đủ thông tin.";
            await OnGetAsync();
            return Page();
        }

        var appointmentDateTime = SelectedDate.Date.Add(TimeSpan.Parse(SelectedTime));
        // Check if slot is available
        var exists = await _context.LichHens.AnyAsync(l =>
            l.MaBacSi == SelectedDoctorId &&
            l.NgayGio == appointmentDateTime &&
            l.TrangThai != "Đã hủy");
        if (exists)
        {
            Message = "Thời gian này đã có lịch hẹn. Vui lòng chọn thời gian khác.";
            await OnGetAsync();
            return Page();
        }

        var appointment = new LichHen
        {
            MaBenhNhan = SelectedPatientId,
            MaBacSi = SelectedDoctorId,
            NgayGio = appointmentDateTime,
            TrieuChung = Symptoms,
            TrangThai = "Chờ xác nhận"
        };
        _context.LichHens.Add(appointment);
        await _context.SaveChangesAsync();
        Message = "Đặt lịch thành công! Vui lòng chờ xác nhận.";
        await OnGetAsync();
        return Page();
    }
}