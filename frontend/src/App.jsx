import { useState, useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import LandingPage from './pages/LandingPage';
import Dashboard from './pages/Dashboard';
import MemberDashboard from './pages/MemberDashboard';
import ProjectView from './pages/ProjectView';
import ProjectsPage from './pages/ProjectsPage';
import KanbanPage from './pages/KanbanPage';
import TeamPage from './pages/TeamPage';
import ReportsPage from './pages/ReportsPage';
import { authService } from './services/api';

function App() {
  const [user, setUser] = useState(null);
  const [token, setToken] = useState(localStorage.getItem('token'));
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (token) {
      validateToken();
    } else {
      setLoading(false);
    }
  }, [token]);

  const validateToken = async () => {
    try {
      const res = await authService.me();
      setUser(res.data);
    } catch (err) {
      console.error("Token validation failed", err);
      handleLogout();
    } finally {
      setLoading(false);
    }
  };

  const handleLogin = (newToken, userData) => {
    localStorage.setItem('token', newToken);
    setToken(newToken);
    setUser(userData);
  };

  const handleLogout = () => {
    localStorage.removeItem('token');
    setToken(null);
    setUser(null);
  };

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background text-primary">
        <div className="w-12 h-12 border-4 border-current border-t-transparent rounded-full animate-spin"></div>
      </div>
    );
  }

  return (
    <Router>
      <Routes>
        <Route 
          path="/" 
          element={user ? <Navigate to={user.role === 'Admin' ? '/dashboard' : '/my-tasks'} /> : <LandingPage onLogin={handleLogin} />} 
        />
        {/* Manager Routes */}
        <Route 
          path="/dashboard" 
          element={user?.role === 'Admin' ? <Dashboard user={user} onLogout={handleLogout} /> : <Navigate to="/" />} 
        />
        <Route 
          path="/projects" 
          element={user?.role === 'Admin' ? <ProjectsPage user={user} onLogout={handleLogout} /> : <Navigate to="/" />} 
        />
        <Route 
          path="/kanban" 
          element={user?.role === 'Admin' ? <KanbanPage user={user} onLogout={handleLogout} /> : <Navigate to="/" />} 
        />
        <Route 
          path="/team" 
          element={user?.role === 'Admin' ? <TeamPage user={user} onLogout={handleLogout} /> : <Navigate to="/" />} 
        />
        <Route 
          path="/reports" 
          element={user?.role === 'Admin' ? <ReportsPage user={user} onLogout={handleLogout} /> : <Navigate to="/" />} 
        />
        
        {/* Member Routes */}
        <Route 
          path="/my-tasks" 
          element={user?.role === 'Member' ? <MemberDashboard user={user} onLogout={handleLogout} /> : <Navigate to="/" />} 
        />
        
        {/* Common Routes */}
        <Route 
          path="/project/:id" 
          element={user ? <ProjectView user={user} onLogout={handleLogout} /> : <Navigate to="/" />} 
        />
      </Routes>
    </Router>
  );
}

export default App;
