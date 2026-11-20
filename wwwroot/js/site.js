document.addEventListener("DOMContentLoaded", function () {
  var today = new Date();
  var tomorrow = new Date(today);
  tomorrow.setDate(today.getDate() + 1);
  function toDateInput(d) {
    return d.toISOString().split("T")[0];
  }

  var checkIn = document.getElementById("checkIn");
  var checkOut = document.getElementById("checkOut");
  if (checkIn && checkOut) {
    checkIn.min = toDateInput(today);
    checkOut.min = toDateInput(tomorrow);
    checkIn.value = toDateInput(today);
    checkOut.value = toDateInput(tomorrow);
    checkIn.addEventListener("change", function () {
      var inDate = new Date(checkIn.value);
      var next = new Date(inDate);
      next.setDate(inDate.getDate() + 1);
      checkOut.min = toDateInput(next);
      if (!checkOut.value || new Date(checkOut.value) <= inDate) {
        checkOut.value = toDateInput(next);
      }
    });
  }

  var bookingForm = document.getElementById("bookingForm");
  if (bookingForm) {
    bookingForm.addEventListener("submit", function (e) {
      e.preventDefault();
      var err = document.getElementById("bookingBarError");
      var ok = document.getElementById("bookingBarSuccess");
      if (err) err.style.display = "none";
      if (ok) ok.style.display = "none";
      if (!checkIn.value || !checkOut.value) {
        if (err) {
          err.textContent = "Please select check-in and check-out dates.";
          err.style.display = "block";
        }
        return;
      }
      var inDate = new Date(checkIn.value);
      var outDate = new Date(checkOut.value);
      if (outDate <= inDate) {
        if (err) {
          err.textContent = "Check-out date must be after check-in date.";
          err.style.display = "block";
        }
        return;
      }
      var nights = Math.round((outDate - inDate) / (1000 * 60 * 60 * 24));
      var guests = document.getElementById("guests").value;
      if (ok) {
        ok.textContent =
          "Showing rooms for " +
          nights +
          " night(s), " +
          guests +
          " guest(s). Please choose a room below.";
        ok.style.display = "block";
      }
      var rooms = document.getElementById("rooms");
      if (rooms) rooms.scrollIntoView({ behavior: "smooth" });
    });
  }

  var contactForm = document.getElementById("contactForm");
  if (contactForm) {
    contactForm.addEventListener("submit", function (e) {
      e.preventDefault();
      var msg = document.getElementById("contactSuccess");
      if (msg) msg.style.display = "block";
      contactForm.reset();
    });
  }

  var observer = new IntersectionObserver(
    function (entries) {
      entries.forEach(function (entry) {
        if (entry.isIntersecting) {
          entry.target.classList.add("visible");
          observer.unobserve(entry.target);
        }
      });
    },
    { threshold: 0.12 },
  );
  document.querySelectorAll(".fade-in").forEach(function (el) {
    observer.observe(el);
  });
});
