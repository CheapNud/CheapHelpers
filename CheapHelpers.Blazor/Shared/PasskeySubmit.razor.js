const browserSupportsPasskeys =
    typeof navigator.credentials !== 'undefined' &&
    typeof window.PublicKeyCredential !== 'undefined' &&
    typeof window.PublicKeyCredential.parseCreationOptionsFromJSON === 'function' &&
    typeof window.PublicKeyCredential.parseRequestOptionsFromJSON === 'function';

// The options endpoints demand an antiforgery token. The form's <AntiforgeryToken /> field supplies it
async function fetchOptions(url, form, signal) {
    const response = await fetch(url, {
        method: 'POST',
        credentials: 'include',
        headers: { 'RequestVerificationToken': new FormData(form).get('__RequestVerificationToken') ?? '' },
        signal,
    });
    if (!response.ok) {
        const text = await response.text();
        console.error(text);
        throw new Error(`The server responded with status ${response.status}.`);
    }
    return await response.json();
}

async function createCredential(url, form, signal) {
    const options = PublicKeyCredential.parseCreationOptionsFromJSON(await fetchOptions(url, form, signal));
    return await navigator.credentials.create({ publicKey: options, signal });
}

async function requestCredential(url, form, mediation, signal) {
    const options = PublicKeyCredential.parseRequestOptionsFromJSON(await fetchOptions(url, form, signal));
    return await navigator.credentials.get({ publicKey: options, mediation, signal });
}

customElements.define('passkey-submit', class extends HTMLElement {
    static formAssociated = true;

    connectedCallback() {
        this.internals = this.attachInternals();
        this.attrs = {
            operation: this.getAttribute('operation'),
            optionsUrl: this.getAttribute('options-url'),
        };

        this.internals.form.addEventListener('submit', (event) => {
            if (event.submitter?.name === '__passkeySubmit') {
                event.preventDefault();
                this.obtainAndSubmitCredential();
            }
        });

        this.tryAutofillPasskey();
    }

    disconnectedCallback() {
        this.abortController?.abort();
    }

    async obtainCredential(useConditionalMediation, signal) {
        if (!browserSupportsPasskeys) {
            throw new Error('Some passkey features are missing. Please update your browser.');
        }

        const form = this.internals.form;
        if (this.attrs.operation === 'Create') {
            return await createCredential(this.attrs.optionsUrl, form, signal);
        } else if (this.attrs.operation === 'Request') {
            const mediation = useConditionalMediation ? 'conditional' : undefined;
            return await requestCredential(this.attrs.optionsUrl, form, mediation, signal);
        } else {
            throw new Error(`Unknown passkey operation '${this.attrs.operation}'.`);
        }
    }

    async obtainAndSubmitCredential(useConditionalMediation = false) {
        this.abortController?.abort();
        this.abortController = new AbortController();
        const signal = this.abortController.signal;
        const formData = new FormData();
        try {
            const credential = await this.obtainCredential(useConditionalMediation, signal);
            const credentialJson = JSON.stringify(credential);
            formData.append('Passkey.CredentialJson', credentialJson);
        } catch (error) {
            if (error.name === 'AbortError') {
                // The user explicitly canceled the operation - return without error.
                return;
            }
            console.error(error);
            if (useConditionalMediation) {
                // An error occurred during conditional mediation, which is not user-initiated.
                // We log the error in the console but do not relay it to the user.
                return;
            }
            // The server ignores this text: a post without a credential gets its own localised message
            const errorMessage = error.name === 'NotAllowedError'
                ? 'No passkey was provided by the authenticator.'
                : error.message;
            formData.append('Passkey.Error', errorMessage);
        }
        this.internals.setFormValue(formData);
        this.internals.form.submit();
    }

    async tryAutofillPasskey() {
        if (browserSupportsPasskeys && this.attrs.operation === 'Request' && await PublicKeyCredential.isConditionalMediationAvailable?.()) {
            await this.obtainAndSubmitCredential(/* useConditionalMediation */ true);
        }
    }
});
