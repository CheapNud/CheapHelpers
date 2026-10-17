// Blazor runs this initializer on every page, static or interactive, so the account pages'
// passkey buttons get their <passkey-submit> element without a script tag in the host page.
export const afterWebStarted = () => import('./Shared/PasskeySubmit.razor.js');
export const afterStarted = afterWebStarted;
