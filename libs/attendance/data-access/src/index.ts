export { AttendanceGateway } from './lib/attendance-gateway';
export type {
  BookableOffice,
  BookableRoom,
  MyReservation,
  OfficeId,
  ReservationId,
  RoomAvailability,
  RoomId,
} from './lib/booking';
export { employeeId, officeId, reservationId, roomId } from './lib/booking';
export type { EmployeeId } from './lib/booking';
export type { Employee } from './lib/employee';
export { toEmployee } from './lib/employee';
export {
  BOOKING_WINDOW_DAYS,
  addDays,
  bookableDaysFrom,
  isBookable,
  isPastDay,
  isWorkingDay,
  todayInBerlin,
} from './lib/bookable-day';
export type { Occupant, OccupancyDay, OccupancyOffice, OccupancyRoom } from './lib/occupancy';
export { toOccupancyDays } from './lib/occupancy';
export type { DateRange, RangePreset } from './lib/occupancy-range';
export { addMonths, isSameMonth, monthGrid, rangeFor } from './lib/occupancy-range';
