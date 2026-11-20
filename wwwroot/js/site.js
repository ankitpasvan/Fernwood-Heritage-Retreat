var bookingForm = document.getElementById("bookingForm");

if (bookingForm) {
  bookingForm.addEventListener("submit", function (e) {
    var err = document.getElementById("bookingBarError");

    if (err) err.style.display = "none";

    if (!checkIn.value || !checkOut.value) {
      e.preventDefault();

      if (err) {
        err.textContent = "Please select check-in and check-out dates.";
        err.style.display = "block";
      }

      return;
    }

    var inDate = new Date(checkIn.value);

    var outDate = new Date(checkOut.value);

    if (outDate <= inDate) {
      e.preventDefault();

      if (err) {
        err.textContent = "Check-out date must be after check-in date.";
        err.style.display = "block";
      }

      return;
    }

    // Dates are valid: let the form submit to /Rooms/Search.
  });
}
