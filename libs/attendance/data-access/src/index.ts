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
export { officeId, reservationId, roomId } from './lib/booking';
export {
  BOOKING_WINDOW_DAYS,
  addDays,
  bookableDaysFrom,
  isBookable,
  isPastDay,
  isWorkingDay,
  todayInBerlin,
} from './lib/bookable-day';
