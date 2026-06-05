import { useMemo } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import {
  MONTH_PICKER_OPTIONS,
  buildYearOptions,
  formatMonthKey,
  getContributionYearRange,
  parseMonthKey,
} from '../utils/contributionMonthRange';
import { SelectDropdown } from './SelectDropdown';
import { colors, spacing } from '../theme';

interface MonthYearSelectProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
}

export function MonthYearSelect({ label, value, onChange }: MonthYearSelectProps) {
  const parsed = parseMonthKey(value) ?? parseMonthKey(formatMonthKey(new Date().getFullYear(), new Date().getMonth() + 1))!;
  const { minYear, maxYear } = useMemo(() => getContributionYearRange(), []);

  const monthValue = String(parsed.month).padStart(2, '0');
  const yearValue = String(parsed.year);

  const yearOptions = useMemo(
    () =>
      buildYearOptions(minYear, maxYear).map((year) => ({
        value: String(year),
        label: String(year),
      })),
    [minYear, maxYear],
  );

  const updateMonth = (nextMonth: string) => {
    onChange(formatMonthKey(parsed.year, Number(nextMonth)));
  };

  const updateYear = (nextYear: string) => {
    onChange(formatMonthKey(Number(nextYear), parsed.month));
  };

  return (
    <View style={styles.wrap}>
      <Text style={styles.groupLabel}>{label}</Text>
      <View style={styles.row}>
        <SelectDropdown
          label="Month"
          value={monthValue}
          options={MONTH_PICKER_OPTIONS}
          onChange={updateMonth}
        />
        <SelectDropdown
          label="Year"
          value={yearValue}
          options={yearOptions}
          onChange={updateYear}
        />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: { marginBottom: spacing.md },
  groupLabel: {
    fontSize: 12,
    fontWeight: '700',
    color: colors.textMuted,
    marginBottom: spacing.sm,
  },
  row: { flexDirection: 'row', gap: spacing.sm },
});
