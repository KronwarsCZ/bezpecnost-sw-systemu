function logout() {
    localStorage.removeItem("login_token");
    alert("Logged out!");
    window.location.href = '/';
}