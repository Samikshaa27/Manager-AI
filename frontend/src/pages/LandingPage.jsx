import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Sparkles, ArrowRight, Briefcase, Users, LogIn, Mail, Lock, X, Loader2 } from 'lucide-react';
import { authService } from '../services/api';
import { cn } from '../lib/utils';

const LandingPage = ({ onLogin }) => {
  const [showLogin, setShowLogin] = useState(false);
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('password123');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [isRegister, setIsRegister] = useState(false);
  const [name, setName] = useState('');
  const [role, setRole] = useState('TeamMember');

  const handleQuickLogin = (role) => {
    const demoEmail = role === 'manager' ? 'manager@managerai.com' : 'member@managerai.com';
    setEmail(demoEmail);
    setIsRegister(false);
    setShowLogin(true);
  };

  const handleLoginSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError('');
    try {
      if (isRegister) {
        await authService.register({ name, email, password, role });
      }
      
      const res = await authService.login(email, password);
      const token = res.data.token;
      localStorage.setItem('token', token);
      const userRes = await authService.me();
      onLogin(token, userRes.data);
    } catch (err) {
      console.error(err);
      setError(isRegister ? 'Registration failed. Try a different email.' : 'Login failed. Please check your credentials.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-transparent dot-grid relative overflow-hidden text-text">
      <div className="absolute inset-0 overflow-hidden pointer-events-none z-0">
        {/* Decorative Blur Orbs */}
        <motion.div 
          animate={{ scale: [1, 1.2, 1], opacity: [0.3, 0.5, 0.3] }}
          transition={{ duration: 8, repeat: Infinity, ease: "easeInOut" }}
          className="absolute top-[-10%] left-[-10%] w-[500px] h-[500px] bg-primary/20 rounded-full blur-[120px]" 
        />
        <motion.div 
          animate={{ scale: [1, 1.5, 1], opacity: [0.2, 0.4, 0.2] }}
          transition={{ duration: 10, repeat: Infinity, ease: "easeInOut", delay: 1 }}
          className="absolute bottom-[-10%] right-[-10%] w-[500px] h-[500px] bg-secondary/20 rounded-full blur-[120px]" 
        />

        {/* Floating AI Nodes */}
        <motion.div 
          animate={{ y: [0, -30, 0], x: [0, 15, 0] }}
          transition={{ duration: 7, repeat: Infinity, ease: "easeInOut" }}
          className="absolute top-[20%] left-[10%] w-16 h-16 bg-white/60 backdrop-blur-md rounded-2xl shadow-[0_8px_32px_rgba(217,70,239,0.15)] border border-primary/20 flex items-center justify-center opacity-80"
        >
          <Briefcase className="text-primary/60" size={24} />
        </motion.div>

        <motion.div 
          animate={{ y: [0, 40, 0], x: [0, -20, 0] }}
          transition={{ duration: 9, repeat: Infinity, ease: "easeInOut", delay: 1 }}
          className="absolute top-[65%] left-[15%] w-20 h-20 bg-white/60 backdrop-blur-md rounded-3xl shadow-[0_8px_32px_rgba(139,92,246,0.15)] border border-secondary/20 flex items-center justify-center opacity-70"
        >
          <Users className="text-secondary/60" size={32} />
        </motion.div>

        <motion.div 
          animate={{ y: [0, -25, 0], x: [0, -25, 0] }}
          transition={{ duration: 8, repeat: Infinity, ease: "easeInOut", delay: 2 }}
          className="absolute top-[25%] right-[12%] w-14 h-14 bg-white/60 backdrop-blur-md rounded-xl shadow-[0_8px_32px_rgba(217,70,239,0.15)] border border-primary/20 flex items-center justify-center opacity-90"
        >
          <Sparkles className="text-primary/60" size={20} />
        </motion.div>

        <motion.div 
          animate={{ y: [0, 25, 0], x: [0, 20, 0] }}
          transition={{ duration: 10, repeat: Infinity, ease: "easeInOut", delay: 0.5 }}
          className="absolute top-[65%] right-[15%] w-24 h-24 bg-white/60 backdrop-blur-md rounded-[2rem] shadow-[0_8px_32px_rgba(139,92,246,0.15)] border border-secondary/20 flex items-center justify-center opacity-60"
        >
          <Briefcase className="text-secondary/50" size={36} />
        </motion.div>

        {/* Connected Graph Lines */}
        <svg className="absolute inset-0 w-full h-full opacity-30" viewBox="0 0 100 100" preserveAspectRatio="none" xmlns="http://www.w3.org/2000/svg">
          <motion.path 
            initial={{ pathLength: 0 }}
            animate={{ pathLength: 1 }}
            transition={{ duration: 3, ease: "easeInOut" }}
            d="M 12 23 Q 35 15 86 28" 
            fill="none" stroke="url(#nodeGrad1)" strokeWidth="0.5" strokeDasharray="2 2"
          />
          <motion.path 
            initial={{ pathLength: 0 }}
            animate={{ pathLength: 1 }}
            transition={{ duration: 3.5, ease: "easeInOut", delay: 0.5 }}
            d="M 17 68 Q 50 80 83 69" 
            fill="none" stroke="url(#nodeGrad2)" strokeWidth="0.5" strokeDasharray="2 2"
          />
          <motion.path 
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ duration: 2, delay: 2 }}
            d="M 12 23 L 17 68" 
            fill="none" stroke="url(#nodeGrad1)" strokeWidth="0.3" strokeDasharray="1 1"
          />
          <motion.path 
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ duration: 2, delay: 2.5 }}
            d="M 86 28 L 83 69" 
            fill="none" stroke="url(#nodeGrad2)" strokeWidth="0.3" strokeDasharray="1 1"
          />
          <defs>
            <linearGradient id="nodeGrad1" x1="0%" y1="0%" x2="100%" y2="100%">
              <stop offset="0%" stopColor="#d946ef" stopOpacity="0.8" />
              <stop offset="100%" stopColor="#8b5cf6" stopOpacity="0.8" />
            </linearGradient>
            <linearGradient id="nodeGrad2" x1="100%" y1="0%" x2="0%" y2="100%">
              <stop offset="0%" stopColor="#d946ef" stopOpacity="0.8" />
              <stop offset="100%" stopColor="#8b5cf6" stopOpacity="0.8" />
            </linearGradient>
          </defs>
        </svg>
      </div>

      {/* Nav */}
      <motion.nav 
        initial={{ y: -50, opacity: 0 }}
        animate={{ y: 0, opacity: 1 }}
        transition={{ duration: 0.8, ease: "easeOut" }}
        className="relative z-10 flex items-center justify-between px-8 py-6 max-w-7xl mx-auto"
      >
        <div className="flex items-center gap-3">
          <div className="relative w-10 h-10 flex items-center justify-center">
            <div className="absolute inset-0 bg-primary rounded-xl blur-md opacity-30"></div>
            <div className="relative w-10 h-10 bg-gradient-to-br from-primary to-secondary rounded-xl flex items-center justify-center shadow-lg shadow-primary/20">
              <Sparkles className="text-white" size={20} />
            </div>
          </div>
          <span className="text-2xl font-heading font-extrabold tracking-tight bg-clip-text text-transparent bg-gradient-to-r from-primary to-secondary">Manager AI</span>
        </div>
        <button 
          onClick={() => setShowLogin(true)}
          className="btn-secondary"
        >
          Sign In
        </button>
      </motion.nav>

      {/* Main Content */}
      <main className="relative z-10 max-w-7xl mx-auto px-8 pt-20 pb-32">
        <div className="text-center mb-20">
          <motion.div
            initial={{ opacity: 0, scale: 0.8 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ duration: 0.8, ease: "easeOut" }}
            className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full bg-primary/10 text-primary text-xs font-bold uppercase tracking-widest mb-8 border border-primary/30 shadow-[0_0_15px_rgba(217,70,239,0.1)] backdrop-blur-md"
          >
            <Sparkles size={14} />
            <span>AI-Driven Project Execution</span>
          </motion.div>

          <motion.h1 
            initial={{ opacity: 0, y: 30 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.2, duration: 0.8 }}
            className="text-6xl md:text-7xl font-heading font-extrabold text-text leading-[1.1] mb-8 max-w-4xl mx-auto drop-shadow-sm"
          >
            Plan, Assign, and Scale with <span className="bg-clip-text text-transparent bg-gradient-to-r from-primary to-secondary italic">Manager AI</span>
          </motion.h1>

          <motion.p
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.4, duration: 0.8 }}
            className="text-xl text-text-muted max-w-2xl mx-auto mb-16"
          >
            Turn ideas into execution with intelligent automation.
          </motion.p>
        </div>

        {/* Action Cards */}
        <div className="grid md:grid-cols-2 gap-8 max-w-4xl mx-auto">
          <motion.div
            initial={{ opacity: 0, x: -50 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ delay: 0.6, duration: 0.8 }}
            whileHover={{ y: -10, scale: 1.02 }}
            className="glass-card group cursor-pointer"
            onClick={() => handleQuickLogin('manager')}
          >
            <div className="w-14 h-14 bg-primary/10 rounded-2xl flex items-center justify-center text-primary mb-6 group-hover:bg-primary group-hover:text-white transition-all duration-300 border border-primary/20 group-hover:shadow-[0_0_20px_rgba(217,70,239,0.3)]">
              <Briefcase size={28} />
            </div>
            <h3 className="text-2xl font-bold mb-3 text-text">Project Manager</h3>
            <p className="text-text-muted mb-8 italic">
              "I want to orchestrate agents, define project goals, and monitor progress dashboards."
            </p>
            <div className="flex items-center gap-2 text-primary font-bold">
              Join as Manager <ArrowRight size={18} className="group-hover:translate-x-1 transition-transform" />
            </div>
          </motion.div>

          <motion.div
            initial={{ opacity: 0, x: 50 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ delay: 0.8, duration: 0.8 }}
            whileHover={{ y: -10, scale: 1.02 }}
            className="glass-card group cursor-pointer"
            onClick={() => handleQuickLogin('member')}
          >
            <div className="w-14 h-14 bg-secondary/10 rounded-2xl flex items-center justify-center text-secondary mb-6 group-hover:bg-secondary group-hover:text-white transition-all duration-300 border border-secondary/20 group-hover:shadow-[0_0_20px_rgba(139,92,246,0.3)]">
              <Users size={28} />
            </div>
            <h3 className="text-2xl font-bold mb-3 text-text">Team Member</h3>
            <p className="text-text-muted mb-8 italic">
              "I want to view my AI-assigned tasks, track my progress, and collaborate effectively."
            </p>
            <div className="flex items-center gap-2 text-secondary font-bold">
              Join as Member <ArrowRight size={18} className="group-hover:translate-x-1 transition-transform" />
            </div>
          </motion.div>
        </div>
      </main>

      {/* Login Modal */}
      <AnimatePresence>
        {showLogin && (
          <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
            <motion.div 
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              onClick={() => setShowLogin(false)}
              className="absolute inset-0 bg-slate-900/40 backdrop-blur-sm"
            />
            <motion.div
              initial={{ scale: 0.9, opacity: 0, y: 20 }}
              animate={{ scale: 1, opacity: 1, y: 0 }}
              exit={{ scale: 0.9, opacity: 0, y: 20 }}
              transition={{ type: "spring", damping: 25, stiffness: 300 }}
              className="relative w-full max-w-md bg-white/90 backdrop-blur-2xl rounded-[32px] p-10 shadow-2xl border border-primary/20"
            >
              <button 
                onClick={() => setShowLogin(false)}
                className="absolute right-6 top-6 p-2 hover:bg-slate-100 rounded-full transition-colors text-text-muted hover:text-text"
              >
                <X size={20} />
              </button>

              <h2 className="text-3xl font-heading font-extrabold mb-2 text-text">
                {isRegister ? 'Create Account' : 'Welcome'}
              </h2>
              <p className="text-text-muted mb-8 font-medium">
                {isRegister ? 'Join Manager AI to start orchestrating projects.' : 'Log in to your account to continue.'}
              </p>

              <form onSubmit={handleLoginSubmit} className="space-y-4">
                {isRegister && (
                  <div className="space-y-2">
                    <label className="text-xs font-bold text-primary uppercase tracking-widest px-1">Full Name</label>
                    <div className="relative">
                      <Users size={18} className="absolute left-4 top-1/2 -translate-y-1/2 text-text-muted" />
                      <input 
                        type="text"
                        required
                        value={name}
                        onChange={e => setName(e.target.value)}
                        placeholder="John Doe"
                        className="w-full bg-white border border-primary/20 shadow-sm rounded-2xl py-3.5 pl-12 pr-4 focus:outline-none focus:ring-2 focus:ring-primary/50 transition-all font-medium text-text placeholder-text-muted"
                      />
                    </div>
                  </div>
                )}
                <div className="space-y-2">
                  <label className="text-xs font-bold text-primary uppercase tracking-widest px-1">Email</label>
                  <div className="relative">
                    <Mail size={18} className="absolute left-4 top-1/2 -translate-y-1/2 text-text-muted" />
                    <input 
                      type="email"
                      required
                      value={email}
                      onChange={e => setEmail(e.target.value)}
                      placeholder="email@company.com"
                      className="w-full bg-white border border-primary/20 shadow-sm rounded-2xl py-3.5 pl-12 pr-4 focus:outline-none focus:ring-2 focus:ring-primary/50 transition-all font-medium text-text placeholder-text-muted"
                    />
                  </div>
                </div>

                <div className="space-y-2">
                  <label className="text-xs font-bold text-primary uppercase tracking-widest px-1">Password</label>
                  <div className="relative">
                    <Lock size={18} className="absolute left-4 top-1/2 -translate-y-1/2 text-text-muted" />
                    <input 
                      type="password"
                      required
                      value={password}
                      onChange={e => setPassword(e.target.value)}
                      placeholder="••••••••"
                      className="w-full bg-white border border-primary/20 shadow-sm rounded-2xl py-3.5 pl-12 pr-4 focus:outline-none focus:ring-2 focus:ring-primary/50 transition-all font-medium text-text placeholder-text-muted"
                    />
                  </div>
                </div>

                {isRegister && (
                  <div className="space-y-2">
                    <label className="text-xs font-bold text-primary uppercase tracking-widest px-1">Initial Role</label>
                    <div className="flex gap-4">
                       <button 
                         type="button"
                         onClick={() => setRole('ProjectManager')}
                         className={cn(
                           "flex-1 py-3 rounded-2xl border text-sm font-bold transition-all shadow-sm",
                           role === 'ProjectManager' ? "bg-primary border-primary text-white" : "bg-white border-primary/20 text-text-muted hover:border-primary/50 hover:text-primary"
                         )}
                       >
                         Manager
                       </button>
                       <button 
                         type="button"
                         onClick={() => setRole('TeamMember')}
                         className={cn(
                           "flex-1 py-3 rounded-2xl border text-sm font-bold transition-all shadow-sm",
                           role === 'TeamMember' ? "bg-secondary border-secondary text-white" : "bg-white border-primary/20 text-text-muted hover:border-secondary/50 hover:text-secondary"
                         )}
                       >
                         Member
                       </button>
                    </div>
                  </div>
                )}

                {error && (
                  <div className="bg-red-500/20 text-red-100 border border-red-500/50 p-3 rounded-xl text-sm font-semibold flex items-center gap-2">
                    <X size={16} /> {error}
                  </div>
                )}

                <button 
                  type="submit" 
                  disabled={loading}
                  className="btn-primary w-full py-4 text-base flex items-center justify-center gap-2 mt-4"
                >
                  {loading ? <Loader2 className="animate-spin" /> : <LogIn size={20} />}
                  <span>{isRegister ? 'Start Now' : 'Sign In'}</span>
                </button>

                <div className="text-center mt-6">
                   <button 
                    type="button"
                    onClick={() => setIsRegister(!isRegister)}
                    className="text-sm font-bold text-text-muted hover:text-white transition-colors underline-offset-4 hover:underline"
                   >
                     {isRegister ? 'Already have an account? Sign In' : "Don't have an account? Create one"}
                   </button>
                </div>
              </form>
            </motion.div>
          </div>
        )}
      </AnimatePresence>
    </div>
  );
};

export default LandingPage;
