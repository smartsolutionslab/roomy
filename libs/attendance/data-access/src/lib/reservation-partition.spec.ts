import { officeId, reservationId, roomId } from './booking';
import type { MyReservation } from './booking';
import { partitionReservationsByDay } from './reservation-partition';

function reservation(date: string): MyReservation {
  return {
    id: reservationId(date),
    officeId: officeId('office'),
    officeName: 'Office',
    roomId: roomId('room'),
    roomName: 'Room',
    date,
  };
}

describe('partitionReservationsByDay', () => {
  it('splits at today (today is upcoming), upcoming soonest first, past most recent first', () => {
    const items = [
      reservation('2026-06-12'),
      reservation('2026-06-08'),
      reservation('2026-06-10'),
      reservation('2026-06-09'),
    ];

    const { upcoming, past } = partitionReservationsByDay(items, '2026-06-10');

    expect(upcoming.map((reservation) => reservation.date)).toEqual(['2026-06-10', '2026-06-12']);
    expect(past.map((reservation) => reservation.date)).toEqual(['2026-06-09', '2026-06-08']);
  });

  it('returns empty partitions for an empty list', () => {
    expect(partitionReservationsByDay([], '2026-06-10')).toEqual({ upcoming: [], past: [] });
  });
});
