// Fernwood Heritage Retreat - site scripts

// ---------- Booking bar: validate dates, then let the form submit to /Rooms/Search ----------
var bookingForm = document.getElementById("bookingForm");

if (bookingForm) {
  var checkInInput = document.getElementById("checkIn");
  var checkOutInput = document.getElementById("checkOut");
  var bookingBarError = document.getElementById("bookingBarError");

  function toLocalDateString(d) {
    var month = (d.getMonth() + 1).toString().padStart(2, "0");
    var day = d.getDate().toString().padStart(2, "0");
    return d.getFullYear() + "-" + month + "-" + day;
  }

  function showBookingError(message) {
    if (bookingBarError) {
      bookingBarError.textContent = message;
      bookingBarError.style.display = "block";
    }
  }

  // Date pickers: no past dates allowed.
  if (checkInInput && checkOutInput) {
    var today = new Date();
    var tomorrow = new Date();
    tomorrow.setDate(today.getDate() + 1);

    checkInInput.min = toLocalDateString(today);
    checkOutInput.min = toLocalDateString(tomorrow);

    if (!checkInInput.value) checkInInput.value = toLocalDateString(today);
    if (!checkOutInput.value) checkOutInput.value = toLocalDateString(tomorrow);

    checkInInput.addEventListener("change", function () {
      var inDate = new Date(checkInInput.value + "T00:00:00");
      var nextDay = new Date(inDate);
      nextDay.setDate(inDate.getDate() + 1);
      checkOutInput.min = toLocalDateString(nextDay);
      if (
        !checkOutInput.value ||
        new Date(checkOutInput.value + "T00:00:00") <= inDate
      ) {
        checkOutInput.value = toLocalDateString(nextDay);
      }
    });
  }

  bookingForm.addEventListener("submit", function (e) {
    if (bookingBarError) bookingBarError.style.display = "none";

    if (!checkInInput.value || !checkOutInput.value) {
      e.preventDefault();
      showBookingError("Please select check-in and check-out dates.");
      return;
    }

    var inDate = new Date(checkInInput.value + "T00:00:00");
    var outDate = new Date(checkOutInput.value + "T00:00:00");
    var todayStart = new Date();
    todayStart.setHours(0, 0, 0, 0);

    if (inDate < todayStart) {
      e.preventDefault();
      showBookingError("Check-in date cannot be in the past.");
      return;
    }

    if (outDate <= inDate) {
      e.preventDefault();
      showBookingError("Check-out date must be after check-in date.");
      return;
    }

    // Dates are valid: let the form submit to /Rooms/Search.
  });
}

// ---------- Scroll fade-in: sections with .fade-in become visible on scroll ----------
var fadeElements = document.querySelectorAll(".fade-in");

if (fadeElements.length > 0) {
  if ("IntersectionObserver" in window) {
    var observer = new IntersectionObserver(
      function (entries) {
        entries.forEach(function (entry) {
          if (entry.isIntersecting) {
            entry.target.classList.add("visible");
            observer.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.1 },
    );

    fadeElements.forEach(function (el) {
      observer.observe(el);
    });
  } else {
    // Old browser: show everything immediately.
    fadeElements.forEach(function (el) {
      el.classList.add("visible");
    });
  }
}
