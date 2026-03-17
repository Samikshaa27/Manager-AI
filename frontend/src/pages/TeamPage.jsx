import { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import Sidebar from '../components/Sidebar';
import { Users, Mail, Shield, MoreHorizontal, Search, UserPlus } from 'lucide-react';
import { authService } from '../services/api';
import { cn } from '../lib/utils';

const TeamPage = ({ user, onLogout }) => {
  const [team, setTeam] = useState([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');

  useEffect(() => {
    fetchTeam();
  }, []);

  const fetchTeam = async () => {
    try {
      const res = await authService.getUsers();
      setTeam(res.data || []);
    } catch (err) {
      console.error("Failed to fetch team members", err);
    } finally {
      setLoading(false);
    }
  };

  const filteredTeam = team.filter(member => 
    member.name?.toLowerCase().includes(searchTerm.toLowerCase()) ||
    member.email?.toLowerCase().includes(searchTerm.toLowerCase()) ||
    member.role?.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <div className="min-h-screen bg-transparent text-text flex relative">
      <Sidebar user={user} onLogout={onLogout} />
      
      <main className="flex-grow ml-72 p-12 relative z-10 w-full overflow-hidden">
        <motion.header 
          initial={{ opacity: 0, y: -20 }}
          animate={{ opacity: 1, y: 0 }}
          className="flex items-center justify-between mb-12"
        >
          <div>
            <h1 className="text-4xl font-heading font-extrabold mb-1 bg-clip-text text-transparent bg-gradient-to-r from-primary to-secondary">Team Management</h1>
            <p className="text-text-muted font-medium">Coordinate with your AI-assisted workforce.</p>
          </div>
          <div className="flex items-center gap-4">
             <div className="relative">
                <Search className="absolute left-4 top-1/2 -translate-y-1/2 text-text-muted" size={18} />
                <input 
                  type="text" 
                  placeholder="Search members..." 
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="bg-white/60 border border-primary/20 rounded-2xl py-3 pl-12 pr-4 text-sm focus:outline-none focus:ring-2 focus:ring-primary/50 transition-all font-medium min-w-[300px] text-text placeholder-text-muted shadow-sm"
                />
             </div>
             <motion.button 
               whileHover={{ scale: 1.05 }}
               whileTap={{ scale: 0.95 }}
               className="btn-primary gap-2 py-3 px-6 rounded-2xl shadow-sm hover:shadow-[0_4px_15px_rgba(217,70,239,0.3)]"
             >
                <UserPlus size={18} /> Add Member
             </motion.button>
          </div>
        </motion.header>

        {loading ? (
          <div className="glass-card p-20 text-center flex flex-col items-center border-primary/10">
            <div className="animate-spin inline-block w-10 h-10 border-4 border-primary border-t-transparent rounded-full mb-4 shadow-sm"></div>
            <p className="text-primary font-bold uppercase tracking-widest text-sm">Assembling team data...</p>
          </div>
        ) : filteredTeam.length === 0 ? (
          <motion.div 
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className="glass-card p-20 text-center flex flex-col items-center border-dashed border-primary/20 bg-white/50"
          >
            <div className="w-20 h-20 bg-white rounded-full flex items-center justify-center text-primary/40 border border-primary/20 mb-6 shadow-sm">
                <Users size={32} />
            </div>
            <h3 className="text-2xl font-heading font-extrabold mb-2 text-text">No members found</h3>
            <p className="text-text-muted max-w-sm font-medium">
                Try adjusting your search or add new members to your organization.
            </p>
          </motion.div>
        ) : (
          <div className="grid grid-cols-1 xl:grid-cols-2 2xl:grid-cols-3 gap-8 pb-12">
            <AnimatePresence>
              {filteredTeam.map((member, i) => (
                <motion.div 
                  initial={{ opacity: 0, scale: 0.9 }}
                  animate={{ opacity: 1, scale: 1 }}
                  exit={{ opacity: 0, scale: 0.9 }}
                  transition={{ delay: i * 0.05 }}
                  key={member.id} 
                  className="glass-card p-8 border-primary/10 hover:border-primary/30 hover:shadow-[0_10px_30px_rgba(217,70,239,0.15)] group transition-all duration-300 relative overflow-hidden bg-white/60"
                >
                  <div className="absolute top-0 right-0 w-32 h-32 bg-primary/10 rounded-full blur-[50px] -translate-y-1/2 translate-x-1/2 group-hover:bg-primary/20 transition-all opacity-0 group-hover:opacity-100" />
                  
                  <div className="flex items-start justify-between mb-6 relative z-10">
                    <div className={cn(
                      "w-16 h-16 rounded-2xl flex items-center justify-center text-2xl font-bold shadow-sm group-hover:scale-110 transition-transform duration-300",
                      member.role === 'ProjectManager' ? "bg-primary/10 text-primary border border-primary/20 shadow-sm" : "bg-white text-text border border-primary/20"
                    )}>
                      {(member.name || 'U').charAt(0)}
                    </div>
                    <button className="p-2 hover:bg-primary/5 rounded-xl text-text-muted hover:text-primary transition-all">
                      <MoreHorizontal size={20} />
                    </button>
                  </div>

                  <div className="mb-6 relative z-10">
                    <h3 className="text-2xl font-bold text-text mb-2 group-hover:text-transparent group-hover:bg-clip-text group-hover:bg-gradient-to-r group-hover:from-primary group-hover:to-secondary transition-all">{member.name || 'Unknown User'}</h3>
                    <div className={cn(
                      "flex items-center gap-2 text-xs font-bold uppercase tracking-widest px-3 py-1.5 rounded-xl w-fit border backdrop-blur-md shadow-sm",
                      member.role === 'ProjectManager' ? "bg-primary/10 text-primary border-primary/20" : "bg-primary/5 text-text-muted border-primary/10"
                    )}>
                      <Shield size={14} className={member.role === 'ProjectManager' ? "text-primary" : "text-text-muted"} />
                      {member.role === 'ProjectManager' ? 'Lead Manager' : 'Team Associate'}
                    </div>
                  </div>

                  <div className="space-y-4 relative z-10">
                    <div className="flex items-center gap-4 text-sm text-text-muted font-medium group-hover:text-text transition-colors">
                      <div className="p-2 bg-white rounded-lg border border-primary/10 shadow-sm">
                        <Mail size={16} className="text-primary/70" />
                      </div>
                      <span className="truncate">{member.email}</span>
                    </div>
                    <div className="flex items-center gap-4 text-sm text-text-muted font-medium group-hover:text-text transition-colors">
                      <div className="p-2 bg-white rounded-lg border border-primary/10 shadow-sm">
                        <Shield size={16} className="text-orange-500/80" />
                      </div>
                      <span>Permission Level: <strong className="text-primary">{member.role === 'ProjectManager' ? 'High' : 'Standard'}</strong></span>
                    </div>
                  </div>

                  <div className="mt-8 pt-6 border-t border-primary/10 flex items-center justify-between relative z-10">
                     <div className="flex -space-x-3">
                        {[1, 2, 3].map(i => (
                          <div key={i} className="w-10 h-10 rounded-full border-2 border-white bg-primary/10 backdrop-blur-sm flex items-center justify-center text-[10px] text-primary" />
                        ))}
                     </div>
                     <button className="text-sm font-bold text-primary hover:text-white transition-all bg-primary/10 px-4 py-2 rounded-xl border border-primary/20 hover:bg-primary shadow-sm hover:shadow-md">
                        View Profile
                     </button>
                  </div>
                </motion.div>
              ))}
            </AnimatePresence>
          </div>
        )}
      </main>
    </div>
  );
};

export default TeamPage;
