import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';

import { FormField } from '../form-field/form-field';
import { SelectOption } from '../select/select';

let uniqueId = 0;

// An accessible search combobox (WAI-ARIA list autocomplete): a text input over a popup listbox of
// matching options. The caller owns the search — `search` emits the typed query (debounce/fetch upstream)
// and feeds the resulting `options` back; picking one emits `selected` and fills the input with its label.
// Keyboard: ArrowUp/Down move the active option, Enter selects it, Escape closes the list.
@Component({
  selector: 'roomy-combobox',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormField],
  templateUrl: './combobox.html',
  styleUrl: './combobox.css',
})
export class Combobox {
  readonly label = input.required<string>();
  readonly placeholder = input.required<string>();
  readonly options = input.required<readonly SelectOption[]>();
  readonly noResultsText = input.required<string>();
  readonly loading = input(false);
  readonly searchChange = output<string>();
  readonly selected = output<string>();

  private readonly id = `roomy-combobox-${uniqueId++}`;
  protected readonly listboxId = `${this.id}-listbox`;

  protected readonly text = signal('');
  protected readonly open = signal(false);
  protected readonly activeIndex = signal(-1);

  protected readonly listboxOpen = computed(() => this.open() && this.options().length > 0);
  protected readonly showNoResults = computed(
    () => this.open() && !this.loading() && this.text().length > 0 && this.options().length === 0,
  );
  protected readonly activeDescendant = computed(() =>
    this.listboxOpen() && this.activeIndex() >= 0 ? this.optionId(this.activeIndex()) : null,
  );

  protected optionId(index: number): string {
    return `${this.id}-option-${index}`;
  }

  protected onInput(value: string): void {
    this.text.set(value);
    this.activeIndex.set(-1);
    this.open.set(true);
    this.searchChange.emit(value);
  }

  protected onFocus(): void {
    this.open.set(true);
  }

  protected onBlur(): void {
    this.open.set(false);
    this.activeIndex.set(-1);
  }

  protected onKeydown(event: KeyboardEvent): void {
    const lastIndex = this.options().length - 1;
    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.open.set(true);
        this.activeIndex.update((index) => Math.min(index + 1, lastIndex));
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.open.set(true);
        this.activeIndex.update((index) => Math.max(index - 1, 0));
        break;
      case 'Enter': {
        const option = this.options()[this.activeIndex()];
        if (this.listboxOpen() && option) {
          event.preventDefault();
          this.choose(option);
        }
        break;
      }
      case 'Escape':
        this.open.set(false);
        this.activeIndex.set(-1);
        break;
    }
  }

  protected choose(option: SelectOption, event?: Event): void {
    event?.preventDefault();
    this.text.set(option.label);
    this.activeIndex.set(-1);
    this.open.set(false);
    this.selected.emit(option.value);
  }
}
