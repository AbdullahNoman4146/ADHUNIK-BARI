(() => {
    "use strict";

    const root = document.querySelector("[data-property-create]");
    if (!root) return;

    const form = document.getElementById("propertyCreateForm");
    const flatSelect = document.getElementById("FlatId");
    const titleInput = document.getElementById("Title");
    const shortDescription = document.getElementById("ShortDescription");
    const descriptionInput = document.getElementById("Description");
    const priceInput = document.getElementById("Price");
    const advanceInput = document.getElementById("AdvanceAmount");
    const roomImageInput = document.getElementById("RoomLayoutImage");
    const coverImageInput = document.getElementById("CoverImage");
    const featuresInput = document.getElementById("Features");
    const tagInput = document.getElementById("propertyTagInput");
    const tagList = root.querySelector("[data-tag-list]");
    const autosaveState = document.getElementById("propertyAutosaveState");
    const submitButton = document.getElementById("submitListingButton");
    const overlay = document.getElementById("propertySubmitOverlay");
    const publishButton = document.getElementById("publishListingButton");
    const keepDraftButton = document.getElementById("keepDraftButton");
    const returnToFormButton = document.getElementById("returnToFormButton");
    const toast = document.getElementById("propertyCreateToast");
    const availabilityAlert = document.getElementById("flatAvailabilityAlert");
    const recheckButton = document.getElementById("recheckFlatsButton");
    const storageKey = "adhunik-bari:create-property-listing";
    const maximumImageSize = 5 * 1024 * 1024;
    const allowedImageTypes = new Set(["image/jpeg", "image/jpg", "image/png", "image/webp"]);
    let createdListing = null;
    let autosaveTimer;
    let toastTimer;
    let stageTimer;
    let roomPreviewUrl;
    let coverPreviewUrl;
    let amenityTags = [];

    const formatMoney = value => {
        const number = Number(value);
        return Number.isFinite(number) && number > 0
            ? new Intl.NumberFormat("en-BD", { maximumFractionDigits: 0 }).format(number)
            : "—";
    };

    const getListingType = () => form.querySelector('input[name="ListingType"]:checked')?.value || "ToLet";
    const selectedFlatLabel = () => flatSelect?.selectedOptions?.[0]?.dataset.flatNumber
        ? `Flat ${flatSelect.selectedOptions[0].dataset.flatNumber}`
        : "the selected flat";

    function showToast(message, icon = "fa-circle-check") {
        const iconNode = toast?.querySelector("i");
        const textNode = toast?.querySelector("span");
        if (!toast || !iconNode || !textNode) return;
        iconNode.className = `fa-solid ${icon}`;
        textNode.textContent = message;
        toast.classList.add("is-visible");
        clearTimeout(toastTimer);
        toastTimer = window.setTimeout(() => toast.classList.remove("is-visible"), 3200);
    }

    function setAutosave(message, saving = false) {
        if (!autosaveState) return;
        autosaveState.classList.toggle("is-saving", saving);
        const text = autosaveState.querySelector("strong");
        if (text) text.textContent = message;
    }

    function serializeLocalDraft() {
        const draft = {};
        Array.from(form.elements).forEach(element => {
            if (!element.name || element.name === "__RequestVerificationToken" || element.type === "file" || element.type === "submit" || element.type === "button") return;
            if ((element.type === "radio" || element.type === "checkbox") && !element.checked) return;
            draft[element.name] = element.value;
        });
        return draft;
    }

    function saveLocalDraft() {
        setAutosave("Saving locally…", true);
        clearTimeout(autosaveTimer);
        autosaveTimer = window.setTimeout(() => {
            try {
                sessionStorage.setItem(storageKey, JSON.stringify(serializeLocalDraft()));
                setAutosave("Draft saved locally just now");
            } catch {
                setAutosave("Changes ready");
            }
        }, 550);
    }

    function restoreLocalDraft() {
        if (titleInput?.value || shortDescription?.value || descriptionInput?.value) return false;
        let draft;
        try {
            draft = JSON.parse(sessionStorage.getItem(storageKey) || "null");
        } catch {
            return false;
        }
        if (!draft) return false;

        Object.entries(draft).forEach(([name, value]) => {
            const fields = form.querySelectorAll(`[name="${CSS.escape(name)}"]`);
            fields.forEach(field => {
                if (field.type === "radio" || field.type === "checkbox") {
                    field.checked = field.value === value;
                } else if (field.tagName === "SELECT") {
                    if (Array.from(field.options).some(option => option.value === value)) field.value = value;
                } else {
                    field.value = value;
                }
            });
        });
        setAutosave("Local draft restored");
        return true;
    }

    function setChecklist(name, ready) {
        root.querySelector(`[data-check="${name}"]`)?.classList.toggle("is-ready", Boolean(ready));
    }

    function updatePreview() {
        const type = getListingType();
        const isToLet = type === "ToLet";
        const title = titleInput?.value.trim();
        document.getElementById("listingPreviewType").textContent = isToLet ? "To-Let" : "For Sale";
        document.getElementById("listingPreviewTitle").textContent = title || "Untitled listing";
        document.getElementById("listingPreviewPrice").textContent = `৳ ${formatMoney(priceInput?.value)}${isToLet ? " /mo" : ""}`;
        document.getElementById("listingPreviewBedrooms").textContent = Math.max(0, Number(document.getElementById("Bedrooms")?.value) || 0);
        document.getElementById("listingPreviewBathrooms").textContent = Math.max(0, Number(document.getElementById("Bathrooms")?.value) || 0);
        document.getElementById("shortDescriptionCount").textContent = shortDescription?.value.length || 0;

        setChecklist("flat", Boolean(flatSelect?.value));
        setChecklist("title", Boolean(title));
        setChecklist("photo", Boolean(roomImageInput?.files?.length));
        setChecklist("price", Number(priceInput?.value) > 0);
    }

    function updateDialogFlat() {
        root.querySelectorAll("[data-dialog-flat]").forEach(node => node.textContent = selectedFlatLabel());
    }

    function clearFieldError(name) {
        const message = form.querySelector(`[data-valmsg-for="${CSS.escape(name)}"]`);
        if (message) {
            message.textContent = "";
            message.classList.remove("field-validation-error");
            message.classList.add("field-validation-valid");
        }
        const field = form.querySelector(`[name="${CSS.escape(name)}"]`)?.closest(".property-create-field");
        field?.classList.remove("has-error");
    }

    function showFieldError(name, message) {
        const normalizedName = name.includes(".") ? name.split(".").pop() : name;
        const fieldInput = form.querySelector(`[name="${CSS.escape(normalizedName)}"]`);
        const validationMessage = form.querySelector(`[data-valmsg-for="${CSS.escape(normalizedName)}"]`);
        fieldInput?.closest(".property-create-field")?.classList.add("has-error");
        if (validationMessage) {
            validationMessage.textContent = message;
            validationMessage.classList.remove("field-validation-valid");
            validationMessage.classList.add("field-validation-error");
        }
        return fieldInput;
    }

    function clearValidation() {
        form.querySelectorAll(".property-create-field.has-error").forEach(field => field.classList.remove("has-error"));
        form.querySelectorAll("[data-valmsg-for]").forEach(message => {
            message.textContent = "";
            message.classList.remove("field-validation-error");
            message.classList.add("field-validation-valid");
        });
        const summary = form.querySelector("[data-validation-summary]");
        if (summary) summary.innerHTML = "";
    }

    function validateBeforeSubmit() {
        clearValidation();
        const errors = [];
        if (!flatSelect?.value) errors.push(["FlatId", "Select an available flat."]);
        if (!titleInput?.value.trim()) errors.push(["Title", "Add a listing title."]);
        if (!shortDescription?.value.trim()) errors.push(["ShortDescription", "Add a short description."]);
        if (!descriptionInput?.value.trim()) errors.push(["Description", "Add the full property description."]);
        if (!(Number(priceInput?.value) > 0)) errors.push(["Price", "Enter a price greater than zero."]);
        if (!(Number(advanceInput?.value) > 0)) errors.push(["AdvanceAmount", "Enter an advance amount greater than zero."]);
        if (!roomImageInput?.files?.length) errors.push(["RoomLayoutImage", "Choose the required room-layout image."]);
        if (roomImageInput?.validationMessage) errors.push(["RoomLayoutImage", roomImageInput.validationMessage]);
        if (coverImageInput?.validationMessage) errors.push(["CoverImage", coverImageInput.validationMessage]);

        let firstInvalid;
        errors.forEach(([name, message]) => {
            const input = showFieldError(name, message);
            firstInvalid ||= input;
        });

        if (errors.length) {
            firstInvalid?.closest(".property-create-card")?.scrollIntoView({ behavior: "smooth", block: "start" });
            window.setTimeout(() => firstInvalid?.focus({ preventScroll: true }), 350);
            showToast("Review the highlighted fields first.", "fa-circle-exclamation");
            return false;
        }
        return true;
    }

    function applyServerErrors(errors) {
        clearValidation();
        let firstInvalid;
        Object.entries(errors || {}).forEach(([name, messages]) => {
            if (!name) return;
            const input = showFieldError(name, Array.isArray(messages) ? messages[0] : String(messages));
            firstInvalid ||= input;
        });
        firstInvalid?.closest(".property-create-card")?.scrollIntoView({ behavior: "smooth", block: "start" });
        window.setTimeout(() => firstInvalid?.focus({ preventScroll: true }), 350);
    }

    function setSubmitStage(name) {
        root.querySelectorAll("[data-submit-stage]").forEach(stage => stage.classList.toggle("d-none", stage.dataset.submitStage !== name));
    }

    function openOverlay(stage) {
        clearTimeout(stageTimer);
        setSubmitStage(stage);
        overlay.classList.add("is-visible");
        overlay.setAttribute("aria-hidden", "false");
        document.body.style.overflow = "hidden";
    }

    function closeOverlay() {
        clearTimeout(stageTimer);
        overlay.classList.remove("is-visible");
        overlay.setAttribute("aria-hidden", "true");
        document.body.style.overflow = "";
    }

    function validateImage(input) {
        const file = input.files?.[0];
        input.setCustomValidity("");
        if (!file) return true;
        if (!allowedImageTypes.has(file.type.toLowerCase())) {
            input.setCustomValidity("Use a JPG, PNG, or WebP image.");
            return false;
        }
        if (file.size > maximumImageSize) {
            input.setCustomValidity("The image must be 5 MB or smaller.");
            return false;
        }
        return true;
    }

    function renderFilePreview(input, dropzone) {
        const file = input.files?.[0];
        const empty = dropzone.querySelector("[data-dropzone-empty]");
        const preview = dropzone.querySelector("[data-dropzone-preview]");
        if (!file || !validateImage(input)) {
            dropzone.classList.remove("has-file");
            empty?.classList.remove("d-none");
            preview?.classList.add("d-none");
            if (input.validationMessage) showFieldError(input.name, input.validationMessage);
            updatePreview();
            return;
        }

        clearFieldError(input.name);
        const oldUrl = input === roomImageInput ? roomPreviewUrl : coverPreviewUrl;
        if (oldUrl) URL.revokeObjectURL(oldUrl);
        const url = URL.createObjectURL(file);
        if (input === roomImageInput) roomPreviewUrl = url;
        else coverPreviewUrl = url;

        dropzone.classList.add("has-file");
        empty?.classList.add("d-none");
        preview?.classList.remove("d-none");
        const image = preview?.querySelector("img");
        if (image) image.src = url;
        const fileName = preview?.querySelector("[data-file-name]");
        const fileSize = preview?.querySelector("[data-file-size]");
        if (fileName) fileName.textContent = file.name;
        if (fileSize) fileSize.textContent = `${Math.max(1, Math.round(file.size / 1024)).toLocaleString()} KB`;

        if (input === roomImageInput) {
            const listingImage = document.getElementById("listingPreviewImage");
            const badgeText = document.getElementById("listingPreviewType")?.textContent || "To-Let";
            listingImage.innerHTML = `<img alt="Room-layout live preview"><b id="listingPreviewType"></b>`;
            listingImage.querySelector("img").src = url;
            listingImage.querySelector("b").textContent = badgeText;
        }
        updatePreview();
        saveLocalDraft();
    }

    function initializeDropzone(input, type) {
        const dropzone = root.querySelector(`[data-dropzone="${type}"]`);
        if (!input || !dropzone) return;
        input.addEventListener("change", () => renderFilePreview(input, dropzone));
        ["dragenter", "dragover"].forEach(eventName => dropzone.addEventListener(eventName, event => {
            event.preventDefault();
            dropzone.classList.add("is-dragging");
        }));
        ["dragleave", "drop"].forEach(eventName => dropzone.addEventListener(eventName, event => {
            event.preventDefault();
            dropzone.classList.remove("is-dragging");
        }));
        dropzone.addEventListener("drop", event => {
            const file = event.dataTransfer?.files?.[0];
            if (!file) return;
            const transfer = new DataTransfer();
            transfer.items.add(file);
            input.files = transfer.files;
            renderFilePreview(input, dropzone);
        });
    }

    function syncAmenityValue() {
        featuresInput.value = amenityTags.join(", ");
        saveLocalDraft();
    }

    function renderAmenityTags() {
        tagList.innerHTML = "";
        amenityTags.forEach((tag, index) => {
            const item = document.createElement("span");
            item.className = "property-amenity-tag";
            item.append(document.createTextNode(tag));
            const remove = document.createElement("button");
            remove.type = "button";
            remove.setAttribute("aria-label", `Remove ${tag}`);
            remove.innerHTML = "&times;";
            remove.addEventListener("click", () => {
                amenityTags.splice(index, 1);
                renderAmenityTags();
                syncAmenityValue();
            });
            item.appendChild(remove);
            tagList.appendChild(item);
        });
    }

    function addAmenity(value) {
        const normalized = value.trim().replace(/^,+|,+$/g, "");
        if (!normalized || amenityTags.some(tag => tag.toLowerCase() === normalized.toLowerCase())) return;
        if (([...amenityTags, normalized].join(", ")).length > 2000) {
            showToast("Amenities must remain under 2,000 characters.", "fa-circle-exclamation");
            return;
        }
        amenityTags.push(normalized);
        renderAmenityTags();
        syncAmenityValue();
    }

    async function recheckAvailableFlats() {
        if (!recheckButton) return;
        const original = recheckButton.innerHTML;
        recheckButton.disabled = true;
        recheckButton.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Checking…';
        try {
            const response = await fetch(root.dataset.availableFlatsUrl, { headers: { "X-Requested-With": "XMLHttpRequest" } });
            const data = await response.json();
            if (!response.ok || !data.success) throw new Error(data.message || "Availability could not be checked.");
            const flats = data.flats || [];
            const currentValue = flatSelect.value;
            flatSelect.innerHTML = '<option value="">Select an available flat…</option>';
            flats.forEach(flat => {
                const flatId = flat.flatId ?? flat.FlatId;
                const flatNumber = flat.flatNumber ?? flat.FlatNumber;
                const floor = flat.floorNumber ?? flat.FloorNumber;
                const rent = Number(flat.monthlyRent ?? flat.MonthlyRent ?? 0);
                const option = document.createElement("option");
                option.value = flatId;
                option.dataset.flatNumber = flatNumber;
                option.dataset.floor = floor;
                option.dataset.rent = rent;
                option.textContent = `Flat ${flatNumber} · Floor ${floor} · Current rent ৳${formatMoney(rent)}`;
                flatSelect.appendChild(option);
            });
            if (flats.some(flat => String(flat.flatId ?? flat.FlatId) === currentValue)) flatSelect.value = currentValue;
            const hasFlats = flats.length > 0;
            availabilityAlert.classList.toggle("d-none", hasFlats);
            flatSelect.classList.toggle("d-none", !hasFlats);
            updatePreview();
            showToast(hasFlats ? `${flats.length} eligible flat${flats.length === 1 ? "" : "s"} available.` : "No eligible flats are available yet.", hasFlats ? "fa-circle-check" : "fa-circle-info");
        } catch (error) {
            showToast(error.message || "Availability could not be checked.", "fa-circle-exclamation");
        } finally {
            recheckButton.disabled = false;
            recheckButton.innerHTML = original;
        }
    }

    async function parseResponse(response) {
        const contentType = response.headers.get("content-type") || "";
        if (contentType.includes("application/json")) return response.json();
        return { success: false, message: "The server returned an unexpected response. Please sign in again and retry." };
    }

    async function submitListing() {
        if (!validateBeforeSubmit()) return;
        submitButton.disabled = true;
        updateDialogFlat();
        openOverlay("submitting");
        stageTimer = window.setTimeout(() => setSubmitStage("checking"), 450);

        try {
            const response = await fetch(form.action, {
                method: "POST",
                body: new FormData(form),
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });
            const data = await parseResponse(response);
            clearTimeout(stageTimer);

            if (!response.ok || !data.success) {
                applyServerErrors(data.errors);
                const flatConflict = Object.keys(data.errors || {}).some(key => key.endsWith("FlatId")) || data.availabilityConfirmed === false;
                if (flatConflict) {
                    document.getElementById("propertySubmitFailureMessage").textContent = data.message || "This flat is no longer eligible. Choose another flat and submit again—your other entries are still here.";
                    setSubmitStage("failure");
                } else {
                    closeOverlay();
                    showToast(data.message || "Review the highlighted fields and try again.", "fa-circle-exclamation");
                }
                return;
            }

            createdListing = data;
            try { sessionStorage.removeItem(storageKey); } catch { }
            setAutosave("Draft saved just now");
            updateDialogFlat();
            setSubmitStage("success");
        } catch {
            clearTimeout(stageTimer);
            closeOverlay();
            showToast("The listing could not be submitted. Check your connection and try again.", "fa-circle-exclamation");
        } finally {
            submitButton.disabled = false;
        }
    }

    async function publishListing() {
        if (!createdListing?.publishUrl) return;
        publishButton.disabled = true;
        setSubmitStage("checking");
        const checkingTitle = root.querySelector('[data-submit-stage="checking"] h2');
        const checkingText = root.querySelector('[data-submit-stage="checking"] p');
        checkingTitle.textContent = "Publishing listing…";
        checkingText.textContent = "Running the final availability check and opening the listing to residents.";

        const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const body = new FormData();
        if (token) body.append("__RequestVerificationToken", token);

        try {
            const response = await fetch(createdListing.publishUrl, {
                method: "POST",
                body,
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });
            const data = await parseResponse(response);
            if (!response.ok || !data.success) {
                document.getElementById("propertySubmitFailureMessage").textContent = data.message || "The listing remains safely saved as a draft. Resolve the availability issue and publish it from Property Listings.";
                setSubmitStage("failure");
                return;
            }
            window.location.assign(data.redirectUrl || root.dataset.indexUrl);
        } catch {
            document.getElementById("propertySubmitFailureMessage").textContent = "Publishing could not be completed. The listing remains safely saved as a draft.";
            setSubmitStage("failure");
        } finally {
            publishButton.disabled = false;
        }
    }

    function initializeProgressRail() {
        const steps = Array.from(root.querySelectorAll("[data-form-step]"));
        const links = Array.from(root.querySelectorAll("[data-step-link]"));
        links.forEach(link => link.addEventListener("click", () => document.getElementById(link.dataset.stepLink)?.scrollIntoView({ behavior: "smooth", block: "start" })));
        if (!("IntersectionObserver" in window)) return;
        const observer = new IntersectionObserver(entries => {
            const visible = entries.filter(entry => entry.isIntersecting).sort((a, b) => b.intersectionRatio - a.intersectionRatio)[0];
            if (!visible) return;
            const index = Number(visible.target.dataset.formStep);
            links.forEach((link, linkIndex) => {
                link.classList.toggle("is-active", linkIndex === index);
                link.classList.toggle("is-complete", linkIndex < index);
            });
        }, { rootMargin: "-18% 0px -52% 0px", threshold: [0, .15, .35, .6] });
        steps.forEach(step => observer.observe(step));
    }

    root.querySelectorAll("[data-stepper]").forEach(button => button.addEventListener("click", () => {
        const input = document.getElementById(button.dataset.stepper);
        const value = Math.max(Number(input.min || 0), (Number(input.value) || 0) + Number(button.dataset.direction || 0));
        input.value = value;
        input.dispatchEvent(new Event("input", { bubbles: true }));
    }));

    form.querySelectorAll('input[name="ListingType"], input[name="FurnishingStatus"]').forEach(input => input.addEventListener("change", () => {
        updatePreview();
        saveLocalDraft();
    }));

    [flatSelect, titleInput, shortDescription, descriptionInput, priceInput, advanceInput].forEach(input => {
        input?.addEventListener("input", () => {
            clearFieldError(input.name);
            updatePreview();
            saveLocalDraft();
        });
        input?.addEventListener("change", () => {
            clearFieldError(input.name);
            updatePreview();
            saveLocalDraft();
        });
    });

    ["Bedrooms", "Bathrooms", "Balconies", "AreaSqFt", "Facing"].forEach(id => document.getElementById(id)?.addEventListener("input", () => {
        updatePreview();
        saveLocalDraft();
    }));

    tagInput?.addEventListener("keydown", event => {
        if ((event.key === "Enter" || event.key === ",") && tagInput.value.trim()) {
            event.preventDefault();
            addAmenity(tagInput.value);
            tagInput.value = "";
        } else if (event.key === "Backspace" && !tagInput.value && amenityTags.length) {
            amenityTags.pop();
            renderAmenityTags();
            syncAmenityValue();
        }
    });
    tagInput?.addEventListener("blur", () => {
        if (tagInput.value.trim()) {
            addAmenity(tagInput.value);
            tagInput.value = "";
        }
    });
    document.getElementById("propertyTagControl")?.addEventListener("click", () => tagInput?.focus());

    form.addEventListener("submit", event => {
        event.preventDefault();
        submitListing();
    });
    recheckButton?.addEventListener("click", recheckAvailableFlats);
    publishButton?.addEventListener("click", publishListing);
    keepDraftButton?.addEventListener("click", () => window.location.assign(createdListing?.keepDraftUrl || root.dataset.indexUrl));
    returnToFormButton?.addEventListener("click", () => {
        closeOverlay();
        recheckAvailableFlats();
        flatSelect?.focus();
    });
    document.addEventListener("keydown", event => {
        if (event.key === "Escape" && overlay.classList.contains("is-visible") && !root.querySelector('[data-submit-stage="submitting"]:not(.d-none), [data-submit-stage="checking"]:not(.d-none)')) closeOverlay();
    });

    if (!form.querySelector('input[name="ListingType"]:checked')) document.getElementById("listingTypeToLet").checked = true;
    if (!form.querySelector('input[name="FurnishingStatus"]:checked')) document.getElementById("furnishingNone").checked = true;
    restoreLocalDraft();
    amenityTags = (featuresInput?.value || "").split(/[,\n]/).map(value => value.trim()).filter(Boolean);
    renderAmenityTags();
    initializeDropzone(roomImageInput, "room");
    initializeDropzone(coverImageInput, "cover");
    initializeProgressRail();
    updatePreview();
})();
