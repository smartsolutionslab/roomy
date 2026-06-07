# roomy — User Stories with Acceptance Criteria

Derived from the four feature specs and all modelling decisions (room-based model: an office contains rooms, each room has a capacity of places, a reservation targets a specific room). Each story lists its acceptance criteria (AC) as nested bullets, condensed from the Given/When/Then scenarios in specs `001`–`004`. Original backlog numbers in parentheses; post-MVP items flagged.

## Epic 001 — Identity & Access

- **IA-1** As an administrator, I want to log in with my email and password, so that I can administer roomy. *(US1)*
  - Correct credentials grant access with administrator privileges.
  - Incorrect credentials are rejected without revealing whether the account exists.
- **IA-2** As an employee, I want to log in with my email and password, so that I can use roomy. *(US5)*
  - Correct credentials grant access with employee privileges.
  - Incorrect credentials are rejected without revealing whether the account exists.
- **IA-3** As an administrator, I want to create employee accounts with an initial password, so that colleagues can use roomy. *(US4)*
  - The admin supplies email, name, and an initial password (at least 8 characters).
  - The new account has the Employee role and can log in immediately.
  - A password shorter than 8 characters is rejected; the email must be unique.
- **IA-4** As an administrator, I want to create additional administrator accounts, so that others can help administer roomy. *(US4)*
  - The admin can grant the Administrator role to a new account.
  - The new administrator has all employee capabilities plus administrative ones.
- **IA-5** As an administrator, I want my account to also be an employee, so that I can plan my own attendance. 
  - Every account, including administrators, has an employee record.
  - An administrator can reserve attendance like any employee.
- **IA-6** As a user, I want to log out, so that my session ends. 
  - After logout, further actions require logging in again.

> Seed: a DefaultAdmin account is provided from configuration so the system can be administered from first start.

## Epic 002 — Office & Room Management *(Admin only)*

- **OR-1** As an administrator, I want to create an office with a name and a location, so that I can set up a site. *(US2)*
  - The office exists after creation (initially without rooms).
  - Office names are unique within the company.
  - A non-administrator cannot create an office.
- **OR-2** As an administrator, I want to edit an office's name and location, so that its details stay correct. *(US3)*
  - Changing the name or location is reflected immediately.
- **OR-3** As an administrator, I want to add rooms with a number of places to an office, so that employees can reserve places in them. 
  - A room is added with a name and a capacity (places); it is then bookable.
  - A capacity below 1 is rejected.
  - Room names are unique within their office.
- **OR-4** As an administrator, I want to rename a room, so that its name stays correct. 
  - Changing a room's name is reflected immediately.
- **OR-5** *(Post-MVP)* As an administrator, I want to change a room's capacity, so that I can adjust available places. 
  - Increasing capacity affects no existing reservation.
  - Reducing below a future day's bookings evicts the most-recently-created reservations (LIFO) until the day fits.
  - The administrator confirms the impact before it is applied.

## Epic 003 — Attendance Planning

- **AT-1** As an employee, I want to reserve a place in a specific room for a working day within the next two weeks, so that I have a guaranteed desk. *(US8)*
  - A place is reserved in the chosen room for a working day within today through today + 14 days.
  - A past day, a weekend, or a day beyond the window is rejected with a clear reason.
- **AT-2** As an employee, I want to book a place for today spontaneously, so that I can come in on short notice. *(US8)*
  - If today is a working day and the room has capacity, a place can be reserved for today.
- **AT-3** As an employee, I want a guaranteed place whenever I reserve, so that I can rely on having a desk. 
  - Reservations for a room on a day never exceed its capacity.
  - With two simultaneous attempts for the last place, exactly one succeeds and capacity is never exceeded.
  - A full room is rejected with a clear "full" message.
- **AT-4** As an employee, I want to cancel my own reservation for today or a future day, so that I free the place. *(US11)*
  - Cancelling frees the place for others.
  - A reservation for a past day cannot be cancelled.
- **AT-5** As an employee, I want to change my reservation by cancelling and re-reserving, so that I can switch room, office, or day. *(US10)*
  - Changing room, office, or day is done as cancel + reserve; there is no single edit step.
- **AT-6** As an administrator, I want to reserve and cancel on behalf of any employee, so that I can manage attendance for others. 
  - The admin can reserve/cancel for any employee under the same booking rules.
  - An employee may only view, not change, another employee's reservation.
  - Booking for an employee who already has a reservation that day is rejected (one per day).

## Epic 004 — Occupancy Views *(read-only)*

- **OC-1** As an employee, I want to see a room's occupancy for a day, week, or month, so that I can choose where to sit. *(US6)*
  - A day shows occupied places against capacity (e.g. 3/8).
  - A week or month shows the figure for each day in the range.
- **OC-2** As an employee, I want an office-level occupancy rollup, so that I can gauge how full an office is. *(US6)*
  - The office figure is the sum of its rooms' occupancy against the sum of their capacities (e.g. 12/30).
- **OC-3** As an employee, I want a calendar view with my own bookings highlighted, so that I can see whether I've already booked a day. *(US7)*
  - The calendar shows each day's occupancy.
  - Days on which the viewer holds a reservation are highlighted.
- **OC-4** As an employee, I want to see who is booked in each room for today and tomorrow, so that I can coordinate with colleagues. 
  - For today and the following day, the names of booked employees are shown per room.
  - For all other days, only counts are shown.
- **OC-5** As an employee, I want to view all my reservations and cancel the upcoming ones, so that I keep an overview and stay in control. *(US9)*
  - All reservations are listed (past, today, future) with office, room, and day.
  - Upcoming reservations can be cancelled from the list; past ones cannot.
- **OC-6** As an employee, I want to view past occupancy read-only, so that I can look back. 
  - Occupancy for past days is viewable and nothing can be changed from that view.

## Cross-cutting rules (apply across the stories)

- **No overbooking:** reservations for a room on a day never exceed its capacity.
- **One reservation per employee per day**, across all rooms and offices.
- **Bookable days only:** working days (Mon–Fri, Europe/Berlin) within today through today + 14 calendar days.
- **Past is immutable:** past reservations cannot be changed or cancelled; past occupancy is view-only.
- **Authorization:** employees manage only their own reservations and may only view others'; administrators may act on behalf of anyone; an administrator is also an employee.
- **Data minimisation:** booked employees' names are shown only for today and the following day.
- **No live updates** in the MVP: views reflect the data at the moment they are opened.

## Notes
- Full acceptance criteria (Given/When/Then) live in the per-feature specs `001`–`004`.
- Post-MVP: room capacity changes and the resulting eviction; self-service password reset; live occupancy updates; notifications; multi-day booking; public-holiday exclusion.
