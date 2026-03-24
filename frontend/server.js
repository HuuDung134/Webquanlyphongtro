const express = require('express');
const path = require('path');
const app = express();
const port = 5253;

// Serve static files from the giaodien directory
app.use(express.static(path.join(__dirname, 'giaodien')));

// Serve index.html for the root route
app.get('/', (req, res) => {
    res.sendFile(path.join(__dirname, 'giaodien', 'pages', 'index.html'));
});

// Start the server
app.listen(port, () => {
    console.log(`Server is running at http://localhost:${port}`);
}); 