// Email form submission
const emailForm = document.getElementById('email-form');

if (emailForm) {
    emailForm.addEventListener('submit', async (e) => {
        e.preventDefault();

        const email = e.target.querySelector('input[type="email"]').value;
        const button = e.target.querySelector('button');
        const originalText = button.textContent;

        // Show loading state
        button.textContent = 'subscribing...';
        button.disabled = true;

        try {
            // TODO: Replace with your actual email collection endpoint
            // For now, just log to console
            console.log('Email submitted:', email);

            // Simulate API call
            await new Promise(resolve => setTimeout(resolve, 1000));

            // Show success
            button.textContent = 'subscribed!';
            e.target.querySelector('input[type="email"]').value = '';

            // Reset after 2 seconds
            setTimeout(() => {
                button.textContent = originalText;
                button.disabled = false;
            }, 2000);
        } catch (error) {
            console.error('Subscription error:', error);
            button.textContent = 'error. try again?';
            button.disabled = false;
        }
    });
}

// Track download clicks (optional)
document.querySelectorAll('.download-btn').forEach(btn => {
    btn.addEventListener('click', () => {
        // TODO: Add analytics tracking here if desired
        console.log('Download clicked');
    });
});
