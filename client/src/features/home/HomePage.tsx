import { Users, Sparkles, ArrowRight, Calendar, MapPin, Star } from "lucide-react";
import { Link } from "react-router";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";

export default function HomePage() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-purple-900 to-slate-900 relative overflow-hidden">
      {/* Animated background elements */}
      <div className="absolute inset-0 overflow-hidden">
        <div className="absolute -top-40 -right-40 w-80 h-80 bg-purple-500/20 rounded-full blur-3xl animate-pulse"></div>
        <div className="absolute -bottom-40 -left-40 w-80 h-80 bg-blue-500/20 rounded-full blur-3xl animate-pulse delay-1000"></div>
        <div className="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 w-96 h-96 bg-teal-500/10 rounded-full blur-3xl animate-pulse delay-500"></div>
      </div>

      {/* Grid pattern overlay */}
      <div className="absolute inset-0 bg-[linear-gradient(rgba(255,255,255,.02)_1px,transparent_1px),linear-gradient(90deg,rgba(255,255,255,.02)_1px,transparent_1px)] bg-[size:50px_50px]"></div>

      <div className="relative z-10 flex flex-col items-center justify-center min-h-screen px-4 text-center">
        {/* Header with logo and title */}
        <div className="mb-8 space-y-6">
          <div className="flex items-center justify-center gap-4 mb-6">
            <div className="relative">
              <div className="absolute inset-0 bg-gradient-to-r from-teal-400 to-purple-500 rounded-2xl blur-lg opacity-75 animate-pulse"></div>
              <div className="relative bg-gradient-to-r from-teal-500 to-purple-600 p-6 rounded-2xl shadow-2xl">
                <Users className="w-16 h-16 text-white" />
              </div>
            </div>
            <div className="space-y-2">
              <h1 className="text-6xl md:text-7xl font-bold bg-gradient-to-r from-white via-teal-200 to-purple-200 bg-clip-text text-transparent">
                HAPPENING
              </h1>
              <div className="flex items-center justify-center gap-2">
                <Sparkles className="w-5 h-5 text-teal-400 animate-pulse" />
                <Badge variant="outline" className="border-teal-400/50 text-teal-300 bg-teal-400/10 backdrop-blur-sm">
                  Connect & Engage
                </Badge>
                <Sparkles className="w-5 h-5 text-purple-400 animate-pulse delay-300" />
              </div>
            </div>
          </div>

          <h2 className="text-2xl md:text-3xl font-light text-gray-300 max-w-2xl mx-auto leading-relaxed">
            Welcome to{" "}
            <span className="bg-gradient-to-r from-teal-400 to-purple-400 bg-clip-text text-transparent font-semibold">
              EventPage
            </span>
          </h2>
        </div>

        {/* Feature highlights */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-12 max-w-4xl mx-auto">
          <div className="group bg-white/5 backdrop-blur-sm border border-white/10 rounded-2xl p-6 hover:bg-white/10 transition-all duration-300 hover:scale-105">
            <Calendar className="w-8 h-8 text-teal-400 mb-3 group-hover:scale-110 transition-transform duration-300" />
            <h3 className="text-lg font-semibold text-white mb-2">Discover Events</h3>
            <p className="text-gray-400 text-sm">Find amazing activities happening around you</p>
          </div>
          
          <div className="group bg-white/5 backdrop-blur-sm border border-white/10 rounded-2xl p-6 hover:bg-white/10 transition-all duration-300 hover:scale-105">
            <MapPin className="w-8 h-8 text-purple-400 mb-3 group-hover:scale-110 transition-transform duration-300" />
            <h3 className="text-lg font-semibold text-white mb-2">Meet People</h3>
            <p className="text-gray-400 text-sm">Connect with like-minded individuals</p>
          </div>
          
          <div className="group bg-white/5 backdrop-blur-sm border border-white/10 rounded-2xl p-6 hover:bg-white/10 transition-all duration-300 hover:scale-105">
            <Star className="w-8 h-8 text-yellow-400 mb-3 group-hover:scale-110 transition-transform duration-300" />
            <h3 className="text-lg font-semibold text-white mb-2">Create Memories</h3>
            <p className="text-gray-400 text-sm">Build lasting experiences together</p>
          </div>
        </div>

        {/* CTA Button */}
        <div className="space-y-4">
          <Button
            asChild
            size="lg"
            className="group relative bg-gradient-to-r from-teal-500 to-purple-600 hover:from-teal-600 hover:to-purple-700 text-white font-semibold px-12 py-6 text-xl rounded-2xl shadow-2xl hover:shadow-teal-500/25 transition-all duration-300 hover:scale-105 active:scale-95"
          >
            <Link to="/events">
              <span className="flex items-center gap-3">
                Take me to the activities!
                <ArrowRight className="w-6 h-6 group-hover:translate-x-1 transition-transform duration-300" />
              </span>
              <div className="absolute inset-0 bg-gradient-to-r from-white/20 to-transparent rounded-2xl opacity-0 group-hover:opacity-100 transition-opacity duration-300"></div>
            </Link>
          </Button>
          
          <p className="text-gray-400 text-sm">
            Join thousands of people creating amazing experiences
          </p>
        </div>

        {/* Floating elements */}
        <div className="absolute top-20 left-10 w-2 h-2 bg-teal-400 rounded-full animate-ping"></div>
        <div className="absolute top-40 right-20 w-1 h-1 bg-purple-400 rounded-full animate-ping delay-700"></div>
        <div className="absolute bottom-32 left-20 w-1.5 h-1.5 bg-yellow-400 rounded-full animate-ping delay-1000"></div>
        <div className="absolute bottom-20 right-10 w-2 h-2 bg-pink-400 rounded-full animate-ping delay-300"></div>
      </div>
    </div>
  );
}